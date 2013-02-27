﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WitMorph.Actions;
using WitMorph.Structures;
using WitMorph.Tests.ProcessTemplates;

namespace WitMorph.Tests
{
    [TestClass]
    public class Convert_Scrum_2_To_Agile_6_Tests
    {
        static readonly IEnumerable<IMorphAction> _actions;

        static Convert_Scrum_2_To_Agile_6_Tests()
        {
            using (var agileTemplate = EmbeddedProcessTemplate.Agile6())
            using (var scrumTemplate = EmbeddedProcessTemplate.Scrum2())
            {
                var agileReader = new ProcessTemplateReader(agileTemplate.TemplatePath);
                var scrumReader = new ProcessTemplateReader(scrumTemplate.TemplatePath);

                var processTemplateMap = ProcessTemplateMap.ConvertScrum2ToAgile6();
                var actionSet = new MorphActionSet();

                var sut = new WitdCollectionComparer(processTemplateMap, actionSet);
                sut.Compare(agileReader.WorkItemTypeDefinitions, scrumReader.WorkItemTypeDefinitions);

                _actions = actionSet.Combine().ToArray();
            }
        }

        private IEnumerable<IMorphAction> Actions
        {
            get { return _actions; }
        }

        [TestMethod]
        public void ScrumToAgile_should_rename_PBI_to_User_Story()
        {
            var renameAction = Actions
                    .OfType<RenameWitdMorphAction>()
                    .SingleOrDefault(a => a.TypeName == "Product Backlog Item" && a.NewName == "User Story");

            Assert.IsNotNull(renameAction);
        }

        [TestMethod]
        public void ScrumToAgile_should_export_and_remove_Impediment()
        {
            var actions = Actions.ToList();

            var exportIndex = actions.FindIndex(a =>
            {
                var e = a as ExportWorkItemDataMorphAction;
                return e != null && e.WorkItemTypeName == "Impediment" && e.AllFields;
            });

            var destroyIndex = actions.FindIndex(a =>
            {
                var d = a as DestroyWitdMorphAction;
                return d != null && d.TypeName == "Impediment";
            });

            Assert.IsTrue(exportIndex > 0, "Is exported");
            Assert.IsTrue(destroyIndex > exportIndex, "Is destroyed after exported");
        }

        [TestMethod]
        public void ScrumToAgile_should_export_extra_Bug_fields()
        {
            var actions = Actions.ToList();

            var exportIndex = actions.FindIndex(a =>
            {
                var e = a as ExportWorkItemDataMorphAction;
                return e != null && e.WorkItemTypeName == "Bug"
                    && e.FieldReferenceNames.Contains("Microsoft.VSTS.Scheduling.Effort")
                    && e.FieldReferenceNames.Contains("Microsoft.VSTS.Common.AcceptanceCriteria");
            });

            Assert.IsTrue(exportIndex > 0, "Is exported");
        }

        [TestMethod]
        public void ScrumToAgile_should_export_extra_Task_fields()
        {
            var actions = Actions.ToList();

            var exportIndex = actions.FindIndex(a =>
            {
                var e = a as ExportWorkItemDataMorphAction;
                return e != null && e.WorkItemTypeName == "Task"
                    && e.FieldReferenceNames.Contains("Microsoft.VSTS.CMMI.Blocked");
            });

            Assert.IsTrue(exportIndex > 0, "Is exported");
        }

        [TestMethod]
        public void ScrumToAgile_should_copy_BacklogPriority_to_StackRank_for_Bug_Task_and_PBI()
        {
            var actions = Actions.ToList();

            var bugCopyIndex = actions.FindIndex(a =>
            {
                var e = a as CopyWorkItemDataMorphAction;
                return e != null && e.TypeName == "Bug" && e.FromField == "Microsoft.VSTS.Common.BacklogPriority" && e.ToField == "Microsoft.VSTS.Common.StackRank";
            });

            var taskCopyIndex = actions.FindIndex(a =>
            {
                var e = a as CopyWorkItemDataMorphAction;
                return e != null && e.TypeName == "Task" && e.FromField == "Microsoft.VSTS.Common.BacklogPriority" && e.ToField == "Microsoft.VSTS.Common.StackRank";
            });

            var pbiCopyIndex = actions.FindIndex(a =>
            {
                var e = a as CopyWorkItemDataMorphAction;
                return e != null && e.TypeName == "Product Backlog Item" && e.FromField == "Microsoft.VSTS.Common.BacklogPriority" && e.ToField == "Microsoft.VSTS.Common.StackRank";
            });

            Assert.IsTrue(bugCopyIndex > 0, "Is Bug field copied");
            Assert.IsTrue(taskCopyIndex > 0, "Is Task field copied");
            Assert.IsTrue(pbiCopyIndex > 0, "Is PBI field copied");
        }

        [TestMethod]
        public void ScrumToAgile_should_copy_Effort_to_StoryPoints_for_PBI()
        {
            var actions = Actions.ToList();

            var pbiCopyIndex = actions.FindIndex(a =>
            {
                var e = a as CopyWorkItemDataMorphAction;
                return e != null && e.TypeName == "Product Backlog Item" && e.FromField == "Microsoft.VSTS.Scheduling.Effort" && e.ToField == "Microsoft.VSTS.Scheduling.StoryPoints";
            });

            Assert.IsTrue(pbiCopyIndex > 0, "Is PBI field copied");
        }


        [TestMethod]
        public void ScrumToAgile_should_modify_Done_state_to_Closed_for_Task()
        {
            var actions = Actions.ToList();

            var taskIndex = actions.FindIndex(a =>
            {
                var e = a as ModifyWorkItemStateMorphAction;
                return e != null && e.TypeName == "Task" && e.FromValue == "Done" && e.ToValue == "Closed";
            });

            Assert.IsTrue(taskIndex > 0, "Is Task state modified");
        }

        [TestMethod]
        public void ScrumToAgile_should_modify_Done_state_to_Resolved_for_PBI()
        {
            var actions = Actions.ToList();

            var pbiIndex = actions.FindIndex(a =>
            {
                var e = a as ModifyWorkItemStateMorphAction;
                return e != null && e.TypeName == "Product Backlog Item" && e.FromValue == "Done" && e.ToValue == "Resolved";
            });

            Assert.IsTrue(pbiIndex > 0, "Is PBI state modified");
        }

        [TestMethod]
        public void ScrumToAgile_should_modify_Done_state_to_Resolved_for_Bug()
        {
            var actions = Actions.ToList();

            var bugIndex = actions.FindIndex(a =>
            {
                var e = a as ModifyWorkItemStateMorphAction;
                return e != null && e.TypeName == "Bug" && e.FromValue == "Done" && e.ToValue == "Resolved";
            });

            Assert.IsTrue(bugIndex > 0, "Is state modified");
        }

        [TestMethod]
        public void ScrumToAgile_should_modify_InProgress_state_to_Active_for_Task()
        {
            var actions = Actions.ToList();

            var taskIndex = actions.FindIndex(a =>
            {
                var e = a as ModifyWorkItemStateMorphAction;
                return e != null && e.TypeName == "Task" && e.FromValue == "In Progress" && e.ToValue == "Active";
            });

            Assert.IsTrue(taskIndex > 0, "Is Task state modified");
        }

        [TestMethod]
        public void ScrumToAgile_should_modify_ToDo_state_to_New_for_Task()
        {
            var actions = Actions.ToList();

            var taskIndex = actions.FindIndex(a =>
            {
                var e = a as ModifyWorkItemStateMorphAction;
                return e != null && e.TypeName == "Task" && e.FromValue == "To Do" && e.ToValue == "New";
            });

            Assert.IsTrue(taskIndex > 0, "Is Task state modified");
        }

        // TODO verify BusinessValue and AcceptanceCriteria are exported for PBIs
        // TODO verify individual fields are removed after export
        // TODO verify extra states are removed 
        // TODO verify state modifications for all WITDs are done before they're removed

    }
}
