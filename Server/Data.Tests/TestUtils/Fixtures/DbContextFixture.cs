// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InstanceService.Data.Tests.TestUtils.Fixtures;
public class DbContextFixture : IDisposable
{
    public DbContextOptions<InstanceServiceDbContext> DbContextOptions { get; }

    public InstanceServiceDbContext DbContext
    {
        get
        {
            var dbContext = new InstanceServiceDbContext(DbContextOptions);
            dbContext.Database.EnsureCreated();
            return dbContext;
        }
    }

    public DbContextFixture()
    {
        DbContextOptions = new DbContextOptionsBuilder<InstanceServiceDbContext>()
            .UseInMemoryDatabase(nameof(DbContextFixture))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    protected virtual void Dispose(bool disposing)
    { }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
