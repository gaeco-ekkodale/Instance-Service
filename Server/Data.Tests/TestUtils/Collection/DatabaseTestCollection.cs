// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Data.Tests.TestUtils.Fixtures;

namespace InstanceService.Data.Tests.TestUtils.Collection;

[CollectionDefinition(nameof(DatabaseTestCollection), DisableParallelization = true)]
public class DatabaseTestCollection : ICollectionFixture<ArcadeDbDatabaseFixture>, ICollectionFixture<DbContextFixture>
{
    // This class has no code and is never created.
    // Its purpose is simply to be the place to apply [CollectionDefinition]
    // and all the ICollectionFixtures<> interfaces for this collection.
}
