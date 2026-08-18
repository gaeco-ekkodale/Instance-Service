// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import { useEffect, useState, useRef } from 'react'
import { Route } from '../../routes/instancesRoute'
import { useQueryClient } from '@tanstack/react-query'
import { enqueueSnackbar } from 'notistack'
import { useAuth } from 'react-oidc-context'
import { jwtDecode } from 'jwt-decode'
import { JwtDTO } from '../../models/JwtDTO'

/**
 * function used to listen to post, put and delete events
 */
export function useNotify() {
	const [connection, setConnection] = useState<null | HubConnection>(null)

	const queryClient = useQueryClient()
	const { useCaseId } = Route.useSearch()
	const auth = useAuth()
	const latestUser = getUsername()
	const latestUseCase = useRef(useCaseId)

	function getUsername() {
		const rawToken = auth.user?.access_token
		if (!rawToken) return undefined
		const token = jwtDecode<JwtDTO>(rawToken)
		return useRef(token.preferred_username)
	}

	useEffect(() => {
		const connect = new HubConnectionBuilder()
			.withUrl(`${import.meta.env.VITE_INSTANCE_SERVER_URL}/hubs/notifications`)
			.withAutomaticReconnect()
			.build()

		setConnection(connect)

		return () => {
			connect.stop()
			setConnection(null)
		}
	})

	useEffect(() => {
		if (!connection || !latestUser) return

		const startConnection = async () => {
			if (connection.state === HubConnectionState.Disconnected) {
				try {
					await connection.start()
				} catch (error) {
					console.error('Connection failed: ', error)
				}
			}
		}

		const setupListeners = () => {
			const handleCreatedNode = (message: string) => {
				enqueueSnackbar(`${message} has been created`, { variant: 'success' })
				if (latestUseCase.current && latestUser.current) {
					queryClient.invalidateQueries({ queryKey: ['nodesGraph', latestUseCase.current] })
				}
			}

			const handleUpdatedNode = (message: string) => {
				enqueueSnackbar(`${message} has been updated`, { variant: 'success' })
				if (latestUseCase.current && latestUser.current) {
					queryClient.invalidateQueries({ queryKey: ['nodesGraph', latestUseCase.current] })
				}
			}

			const handleCreatedRelations = (message: string) => {
				enqueueSnackbar(`${message} has been created`, { variant: 'success' })
				if (latestUseCase.current && latestUser.current) {
					queryClient.invalidateQueries({ queryKey: ['nodesGraph', latestUseCase.current] })
				}
			}

			const handleDeleteNode = (message: string[]) => {
				enqueueSnackbar(`Deleted ${message.length} Instances`, { variant: 'success' })
				if (latestUseCase.current && latestUser.current) {
					queryClient.invalidateQueries({ queryKey: ['nodesGraph', latestUseCase.current] })
				}
			}

			// TODO: event names should be adjusted when its defined in the backend
			connection.on('TODO', handleCreatedNode)
			connection.on('TODO', handleUpdatedNode)
			connection.on('TODO', handleCreatedRelations)
			connection.on('TODO', handleDeleteNode)

			return () => {
				connection.off('TODO', handleCreatedNode)
				connection.off('TODO', handleUpdatedNode)
				connection.off('TODO', handleCreatedRelations)
				connection.off('TODO', handleDeleteNode)
			}
		}

		startConnection()
		const cleanupListeners = setupListeners()

		return () => {
			if (connection.state !== HubConnectionState.Disconnected) {
				connection.stop()
			}
			cleanupListeners()
		}
	}, [connection])

	return null
}
