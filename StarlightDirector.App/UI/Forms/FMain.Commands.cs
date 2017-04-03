﻿using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using StarlightDirector.Commanding;

namespace StarlightDirector.App.UI.Forms {
    partial class FMain {

        private void RegisterCommands() {
            mnuFileOpen.Attach(CmdFileOpen);
            mnuFileSave.Attach(CmdFileSave);
            mnuFileExit.Attach(CmdFileExit);
            sysClose.Attach(CmdFileExit);

            CommandManager.HookForm(this);
            RegisterCommandEvents(this);
        }

        private void UnregisterCommands() {
            CommandManager.UnhookForm(this);
        }

        private static void RegisterCommandEvents(Form form) {
            var formType = form.GetType();
            var commandType = typeof(Command);
            const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            const BindingFlags publicInstance = BindingFlags.Instance | BindingFlags.Public;
            var commandFields = formType.GetFields(privateInstance)
                .Where(field => field.FieldType == commandType || field.FieldType.IsSubclassOf(commandType));
            foreach (var field in commandFields) {
                var commandObject = field.GetValue(form) as Command;
                if (commandObject == null) {
                    continue;
                }
                var commandName = field.Name;
                SubscribeEvent(commandObject, commandName, "Executed", typeof(EventHandler<EventArgs>));
                SubscribeEvent(commandObject, commandName, "Reverted", typeof(EventHandler<EventArgs>));
                SubscribeEvent(commandObject, commandName, "QueryCanExecute", typeof(QueryCanExecuteEventHandler));
                SubscribeEvent(commandObject, commandName, "QueryCanRevert", typeof(QueryCanRevertEventHandler));
                SubscribeEvent(commandObject, commandName, "QueryRecordToHistory", typeof(QueryRecordToHistoryEventHandler));
            }

            void SubscribeEvent(Command commandObject, string commandName, string eventName, Type handlerType)
            {
                var handlerName = commandName + "_" + eventName;
                MethodInfo handlerMethod;
                try {
                    handlerMethod = formType.GetMethod(handlerName, privateInstance);
                } catch (AmbiguousMatchException) {
                    return;
                }
                if (handlerMethod == null) {
                    return;
                }
                var cmdType = commandObject.GetType();
                var eventField = cmdType.GetEvent(eventName, publicInstance | privateInstance);
                if (eventField == null) {
                    return;
                }
                var methodDelegate = Delegate.CreateDelegate(handlerType, form, handlerMethod);
                eventField.AddEventHandler(commandObject, methodDelegate);
            }
        }

    }
}
