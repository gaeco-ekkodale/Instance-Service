// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { HttpTransportType, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { INSTANCE_QUERY_KEYS } from '../../features/instances/useInstanceEdits'

/** Hub served by the InstanceService. */
const HUB_PATH = '/hubs/graph'

/** Server method telling us that a use case changed. */
const GRAPH_CHANGED_EVENT = 'GraphChanged'

/** Hub method asking for the changes of one use case. */
const SUBSCRIBE_METHOD = 'Subscribe'

/**
 * Refetches the instances of the open use case once it changed.
 *
 * The connection carries no token. A notification is nothing but the ID of the use case that is
 * already open here, and the graph itself is fetched over the authenticated API with this user's
 * own token - so what arrives through the hub is a hint to refetch, never data.
 *
 * Changes are collected server side, so one notification stands for a burst of writes. The client
 * that caused a change is notified as well and refetches twice: once on its own write and once
 * when the announcement arrives.
 *
 * Unsaved values are not affected. They live in the edit buffer and are laid over whatever the
 * server returns, so a refresh caused by someone else leaves them untouched.
 *
 * @param useCaseId - The use case being looked at, or nothing while none is picked.
 */
export const useGraphChangeNotifications = (useCaseId?: string | null) => {
	const queryClient = useQueryClient()

	useEffect(() => {
		// Without a use case there is no graph to keep up to date.
		if (!useCaseId) return

		// One connection per use case: switching runs the cleanup below and connects again,
		// which is cheap enough to not have to move a connection between groups.
		let abandoned = false

		const connection = new HubConnectionBuilder()
			// Web sockets only, so a blocked upgrade fails loudly instead of falling back to long
			// polling, which would keep polling for as long as the view is open. Credentials are
			// left out of the negotiation: the service answers any origin with a wildcard, and a
			// browser rejects that as soon as credentials are involved.
			.withUrl(`${import.meta.env.VITE_INSTANCE_SERVER_URL}${HUB_PATH}`, {
				withCredentials: false,
				transport: HttpTransportType.WebSockets,
			})
			.configureLogging(LogLevel.Warning)
			.withAutomaticReconnect()
			.build()

		/**
		 * Notifications are an addition: without them both views still work, they are just no
		 * longer refreshed by the changes of others. Stopping a connection that is still
		 * starting rejects the start, which is the cleanup doing its job rather than a failure.
		 */
		const reportUnavailable = (error: unknown) => {
			if (abandoned) return

			console.error('Graph change notifications unavailable:', error)
		}

		const refetchInstances = () => {
			INSTANCE_QUERY_KEYS.forEach(key => queryClient.invalidateQueries({ queryKey: [key] }))
		}

		connection.on(GRAPH_CHANGED_EVENT, refetchInstances)

		// Following a use case belongs to the connection and is gone once it dropped, so it is
		// asked for again. Whatever was written while the connection was down was announced to
		// nobody, which is why the graph is refetched as well.
		connection.onreconnected(() => {
			connection.invoke(SUBSCRIBE_METHOD, useCaseId).catch(reportUnavailable)
			refetchInstances()
		})

		connection
			.start()
			// The use case was switched or the view left while connecting.
			.then(() => (abandoned ? undefined : connection.invoke(SUBSCRIBE_METHOD, useCaseId)))
			.catch(reportUnavailable)

		return () => {
			abandoned = true
			connection.stop().catch(error => {
				console.error('Failed to close the graph notification connection:', error)
			})
		}
	}, [useCaseId, queryClient])
}
