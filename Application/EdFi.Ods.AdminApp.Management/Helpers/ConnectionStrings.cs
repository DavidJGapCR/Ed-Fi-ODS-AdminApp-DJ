// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApp.Management.Helpers
{
    public class ConnectionStrings
    {
        public string Admin { get; set; }
        public string Security { get; set; }
        public string ProductionOds { get; set; }

        public string GetConnectionStringByName(string databaseName)
        {
            switch (databaseName)
            {
                case CloudOdsDatabaseNames.Admin:
                    return Admin;
                case CloudOdsDatabaseNames.Security:
                    return Security;
                case CloudOdsDatabaseNames.ProductionOds:
                    return ProductionOds;
                default:
                    return null;
            }
        }
    }
}
