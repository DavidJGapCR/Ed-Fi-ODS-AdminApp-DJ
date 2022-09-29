// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using NUnit.Framework;
using EdFi.Ods.AdminApp.Management.ClaimSetEditor;
using Shouldly;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;
using Application = EdFi.Security.DataAccess.Models.Application;
using EdFi.Ods.AdminApp.Web.Models.ViewModels.ClaimSets;
using EdFi.Security.DataAccess.Contexts;
using static EdFi.Ods.AdminApp.Management.Tests.Testing;

namespace EdFi.Ods.AdminApp.Management.Tests.ClaimSetEditor
{
    [TestFixture]
    public class EditClaimSetCommandTests : SecurityDataTestBase
    {
        [Test]
        public void ShouldEditClaimSet()
        {
            var testApplication = new Application
            {
                ApplicationName = $"Test Application {DateTime.Now:O}"
            };
            Save(testApplication);

            var alreadyExistingClaimSet = new ClaimSet { ClaimSetName = "TestClaimSet", Application = testApplication };
            Save(alreadyExistingClaimSet);

            var editModel = new EditClaimSetModel {ClaimSetName = "TestClaimSetEdited", ClaimSetId = alreadyExistingClaimSet.ClaimSetId};

            Scoped<ISecurityContext>(securityContext =>
            {
                var command = new EditClaimSetCommand(securityContext);
                command.Execute(editModel);
            });

            var editedClaimSet = Transaction(securityContext => securityContext.ClaimSets.Single(x => x.ClaimSetId == alreadyExistingClaimSet.ClaimSetId));
            editedClaimSet.ClaimSetName.ShouldBe(editModel.ClaimSetName);
        }

        [Test]
        public void ShouldNotEditClaimSetIfNameNotUnique()
        {
            var testApplication = new Application
            {
                ApplicationName = $"Test Application {DateTime.Now:O}"
            };
            Save(testApplication);

            var alreadyExistingClaimSet = new ClaimSet { ClaimSetName = "TestClaimSet1", Application = testApplication };
            Save(alreadyExistingClaimSet);

            var testClaimSet = new ClaimSet { ClaimSetName = "TestClaimSet2", Application = testApplication };
            Save(testClaimSet);

            var editModel = new EditClaimSetModel { ClaimSetName = "TestClaimSet1", ClaimSetId = testClaimSet.ClaimSetId };

            Scoped<ISecurityContext>(securityContext =>
            {
                var validator = new EditClaimSetModelValidator(securityContext);
                var validationResults = validator.Validate(editModel);
                validationResults.IsValid.ShouldBe(false);
                validationResults.Errors.Single().ErrorMessage.ShouldBe("A claim set with this name already exists in the database. Please enter a unique name.");
            });
        }

        [Test]
        public void ShouldNotEditClaimSetIfNameEmpty()
        {
            var testApplication = new Application
            {
                ApplicationName = $"Test Application {DateTime.Now:O}"
            };
            Save(testApplication);

            var testClaimSet = new ClaimSet { ClaimSetName = "TestClaimSet1", Application = testApplication };
            Save(testClaimSet);

            var editModel = new EditClaimSetModel { ClaimSetName = "", ClaimSetId = testClaimSet.ClaimSetId };

            Scoped<ISecurityContext>(securityContext =>
            {
                var validator = new EditClaimSetModelValidator(securityContext);
                var validationResults = validator.Validate(editModel);
                validationResults.IsValid.ShouldBe(false);
                validationResults.Errors.Single().ErrorMessage.ShouldBe("'Claim Set Name' must not be empty.");
            });
        }

        [Test]
        public void ShouldNotEditClaimSetIfNameLengthGreaterThan255Characters()
        {
            var testApplication = new Application
            {
                ApplicationName = $"Test Application {DateTime.Now:O}"
            };
            Save(testApplication);

            var testClaimSet = new ClaimSet { ClaimSetName = "TestClaimSet1", Application = testApplication };
            Save(testClaimSet);

            var editModel = new EditClaimSetModel { ClaimSetName = "ThisIsAClaimSetWithNameLengthGreaterThan255CharactersThisIsAClaimSetWithNameLengthGreaterThan255CharactersThisIsAClaimSetWithNameLengthGreaterThan255CharactersThisIsAClaimSetWithNameLengthGreaterThan255CharactersThisIsAClaimSetWithNameLengthGreaterThan255CharactersThisIsAClaimSetWithNameLengthGreaterThan255Characters", ClaimSetId = testClaimSet.ClaimSetId };

            Scoped<ISecurityContext>(securityContext =>
            {
                var validator = new EditClaimSetModelValidator(securityContext);
                var validationResults = validator.Validate(editModel);
                validationResults.IsValid.ShouldBe(false);
                validationResults.Errors.Single().ErrorMessage.ShouldBe("The claim set name must be less than 255 characters.");
            });
        }

        [Test]
        public void ShouldNotEditClaimSetIfNotAnExistingId()
        {
            EnsureZeroClaimSets();

            var editModel = new EditClaimSetModel
            {
                ClaimSetName = "Not Existing ClaimSet",
                ClaimSetId = 1
            };

            Scoped<ISecurityContext>(securityContext =>
            {
                var validator = new EditClaimSetModelValidator(securityContext);
                var validationResults = validator.Validate(editModel);
                validationResults.IsValid.ShouldBe(false);
                validationResults.Errors.Single().ErrorMessage.ShouldBe("No such claim set exists in the database");
            });

            void EnsureZeroClaimSets()
            {
                Scoped<ISecurityContext>(database =>
                {
                    foreach (var entity in database.ClaimSets)
                        database.ClaimSets.Remove(entity);
                    database.SaveChanges();
                });
            }
        }

        [Test]
        public void ShouldNotEditClaimSetIfNotEditable()
        {
            var testApplication = new Application
            {
                ApplicationName = $"Test Application {DateTime.Now:O}"
            };
            Save(testApplication);

            var systemReservedClaimSet = new ClaimSet
            {
                ClaimSetName = "SystemReservedClaimSet",
                Application = testApplication,
                ForApplicationUseOnly = true
            };
            Save(systemReservedClaimSet);

            var edfiPresetClaimSet = new ClaimSet
            {
                ClaimSetName = "EdfiPresetClaimSet",
                Application = testApplication,
                ForApplicationUseOnly = false,
                IsEdfiPreset = true
            };
            Save(edfiPresetClaimSet);

            var systemReservedAndEdfiPresetClaimSet = new ClaimSet
            {
                ClaimSetName = "SystemReservedAndEdfiPresetClaimSet",
                Application = testApplication,
                ForApplicationUseOnly = true,
                IsEdfiPreset = true
            };
            Save(systemReservedAndEdfiPresetClaimSet);

            var editableClaimSet = new ClaimSet
            {
                ClaimSetName = "EditableClaimSet",
                Application = testApplication,
                ForApplicationUseOnly = false,
                IsEdfiPreset = false
            };
            Save(editableClaimSet);

            var systemReservedClaimSetEditModel = new EditClaimSetModel
            {
                ClaimSetName = systemReservedClaimSet.ClaimSetName, ClaimSetId = systemReservedClaimSet.ClaimSetId
            };

            var edfiPresetClaimSetEditModel = new EditClaimSetModel
            {
                ClaimSetName = systemReservedClaimSet.ClaimSetName, ClaimSetId = systemReservedClaimSet.ClaimSetId
            };

            var systemReservedAndEdfiPresetClaimSetEditModel = new EditClaimSetModel
            {
                ClaimSetName = systemReservedAndEdfiPresetClaimSet.ClaimSetName, ClaimSetId = systemReservedAndEdfiPresetClaimSet.ClaimSetId
            };

            var editableClaimSetEditModel = new EditClaimSetModel
            {
                ClaimSetName = editableClaimSet.ClaimSetName, ClaimSetId = editableClaimSet.ClaimSetId
            };

            Scoped<ISecurityContext>(securityContext =>
            {
                var validator = new EditClaimSetModelValidator(securityContext);
                var validationResults = validator.Validate(systemReservedClaimSetEditModel);
                validationResults.IsValid.ShouldBe(false);
                validationResults.Errors.Single().ErrorMessage.ShouldBe("Only user created claim sets can be edited");

                validationResults = validator.Validate(edfiPresetClaimSetEditModel);
                validationResults.IsValid.ShouldBe(false);
                validationResults.Errors.Single().ErrorMessage.ShouldBe("Only user created claim sets can be edited");

                validationResults = validator.Validate(systemReservedAndEdfiPresetClaimSetEditModel);
                validationResults.IsValid.ShouldBe(false);
                validationResults.Errors.Single().ErrorMessage.ShouldBe("Only user created claim sets can be edited");

                validationResults = validator.Validate(editableClaimSetEditModel);
                validationResults.IsValid.ShouldBe(true);
                validationResults.Errors.ShouldBeEmpty();
            });
        }

    }
}
