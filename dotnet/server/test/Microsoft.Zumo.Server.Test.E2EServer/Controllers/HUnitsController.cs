﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Zumo.Server.Entity;
using Microsoft.Zumo.Server.Test.E2EServer.Database;
using Microsoft.Zumo.Server.Test.E2EServer.DataObjects;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Zumo.Server.Test.E2EServer.Controllers
{
    [Route("tables/[controller]")]
    [ApiController]
    public class HUnitsController : TableController<HUnit>
    {
        public HUnitsController(E2EDbContext context)
        {
            TableControllerOptions = new TableControllerOptions<HUnit> { SoftDeleteEnabled = false };
            TableRepository = new EntityTableRepository<HUnit>(context);
        }
    }
}