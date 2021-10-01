﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SkySwordKill.Next
{
    public class DialogCommand
    {
        public string command;
        public string[] paramList;
        
        public string charID;
        public string say;

        public DialogEventData bindEventData;

        public bool isEnd;

        public string rawCommand;

        public int GetInt(int index)
        {
            if (index > paramList.Length - 1)
                return 0;
            return Convert.ToInt32(paramList[index]);
        }
        
        public string GetStr(int index)
        {
            if (index > paramList.Length - 1)
                return string.Empty;
            return paramList[index];
        }
    }

    public class DialogOptionCommand
    {
        public string option;
        public string tagEvent;
        public string condition;
    }
    
    public class DialogEventData
    {
        #region 字段

        public string id;
        public Dictionary<string, int> character;
        public string[] dialog;
        public string[] option;

        #endregion

        #region 属性



        #endregion

        #region 回调方法



        #endregion

        #region 公共方法

        public DialogCommand GetDialogCommand(int index,DialogEnvironment env)
        {
            var rawText = dialog[index];
            
            var command = new DialogCommand();
            command.bindEventData = this;
            command.isEnd = index == dialog.Length - 1;
            command.rawCommand = rawText;
            var evaluateText = DialogAnalysis.AnalysisInlineScript(rawText, env);
            
            var strArr = evaluateText.Split('*');
            var posSharp = evaluateText.IndexOf('#');
            var posStar = evaluateText.IndexOf('*');
            
            // 确保第一个星号在井号前面
            if (strArr.Length >= 2 && (posStar < posSharp || posSharp == -1))
            {
                command.command = strArr[0];
                var body = string.Join("*", strArr.Where((s, i) => i > 0));
                command.paramList = body.Split('#');
            }
            else
            {
                command.command = "";
                var body = strArr[0];
                command.paramList = body.Split('#');

                if (command.paramList.Length >= 2)
                {
                    command.charID = command.paramList[0];
                    command.say = command.paramList[1];
                }
            }

            return command;
        }

        public DialogOptionCommand[] GetOptionCommands()
        {
            var optionCommands = new DialogOptionCommand[option.Length];
            for (int i = 0; i < optionCommands.Length; i++)
            {
                var body = option[i].Split('#');
                var curOption = new DialogOptionCommand();
                curOption.option = body[0];
                if (body.Length >= 2)
                    curOption.tagEvent = body[1];
                else
                    curOption.tagEvent = "";
                if (body.Length >= 3)
                    curOption.condition = body[2];
                else
                    curOption.condition = "";
                optionCommands[i] = curOption;
            }

            return optionCommands;
        }

        #endregion

        #region 私有方法



        #endregion


    }
}