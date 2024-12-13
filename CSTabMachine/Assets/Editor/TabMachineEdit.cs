using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CompileTabMachine
{
    [MenuItem("TabMachine/Compile Code", false, 1)]
    public static void CompileAll()
    {
        Type[] types = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => a.GetTypes().Where(t => t.BaseType == typeof(Tab)))
        .ToArray();
        Dictionary<string, int> paramsTypeMap = new Dictionary<string, int>();
        for (int i = 0; i < types.Length; i++)
        {
            Type type = types[i];
            CompileSingle(type, paramsTypeMap);
        }
        string ouputResultStr = "";
        string ouputMethods = CreateOutputMethodsStr(paramsTypeMap, out ouputResultStr);
        string notifyParentDoNextMethod = CreateNotifyParentDoNextMethodStr(paramsTypeMap);
        string callMethods = CreateCallMethodsStr(paramsTypeMap);
        string starMethods = CreateStarMethodsStr(paramsTypeMap);
        string doNextMethods = CreateDoNextMethodsStr(paramsTypeMap);

        string fileContent = $@"
using System;
public partial class Tab
{{
    {ouputResultStr}
    {notifyParentDoNextMethod}
    {ouputMethods}
    {callMethods}
    {starMethods}
    {doNextMethods}
}}
";
        File.WriteAllText("Assets/Scripts/TabMachineWrap/Tab_warp.cs", fileContent);
    }
    private static string CreateOutputMethodsStr(Dictionary<string, int> paramsTypeMap, out string outputValStr)
    {
        StringWriter stringWriter = new StringWriter();
        StringWriter stringWriter1 = new StringWriter();
        foreach (KeyValuePair<string, int> pairs in paramsTypeMap)
        {
            if (pairs.Key.IndexOf(',') != -1)
            {
                stringWriter.WriteLine($"    private ValueTuple<{pairs.Key}> _result{pairs.Value};");
            }
            string[] typesList = pairs.Key.Split(',');
            string strPrams = "";
            string strInvokePrams = "";
            for (int i = 0; i < typesList.Length; i++)
            {
                strPrams += (typesList[i] + " p" + (i + 1));
                strInvokePrams += ("p" + (i + 1));
                if (i < typesList.Length - 1)
                {
                    strPrams += (",");
                    strInvokePrams += (", ");
                }
            }
            string outputMethod = $@"
    public void Output({strPrams})
    {{
        _resultIndex = {pairs.Value};
        _result{pairs.Value} = ({strInvokePrams});
    }}
";
            stringWriter.Write(outputMethod);
        }

        outputValStr = stringWriter.ToString();
        stringWriter.Dispose();
        string outputMethods = stringWriter1.ToString();
        stringWriter1.Dispose();
        return outputMethods;
    }
    private static string CreateCallMethodsStr(Dictionary<string, int> paramsTypeMap)
    {
        StringWriter stringWriter = new StringWriter();

        string callMethod = $@"
    public void Call(Tab tab, string stepName)
    {{
        CreateMainStep(tab, stepName, true);
        tab.Start(""s1"");
    }}
";
        stringWriter.Write(callMethod);
        foreach (KeyValuePair<string, int> pairs in paramsTypeMap)
        {
            string[] typesList = pairs.Key.Split(',');
            string strPrams = "";
            string strInvokePrams = "";
            for (int i = 0; i < typesList.Length; i++)
            {
                strPrams += (typesList[i] + " p" + (i + 1));
                strInvokePrams += ("p" + (i + 1));
                if (i < typesList.Length - 1)
                {
                    strPrams += (",");
                    strInvokePrams += (", ");
                }
            }
            callMethod = $@"
    public void Call(Tab tab, string stepName, {strPrams})
    {{
        CreateMainStep(tab, stepName, true);
        tab.Start(""s1"", {strInvokePrams});
    }}
";
            stringWriter.Write(callMethod);
        }
        string callMethods = stringWriter.ToString();
        stringWriter.Dispose();
        return callMethods;
    }
    private static string CreateStarMethodsStr(Dictionary<string, int> paramsTypeMap)
    {
        string startMethod = $@"
    public virtual void Start(string stepName)
    {{
    }}
";
        StringWriter stringWriter = new StringWriter();
        stringWriter.Write(startMethod);
        foreach (KeyValuePair<string, int> pairs in paramsTypeMap)
        {
            string[] typesList = pairs.Key.Split(',');
            string strPrams = "";
            string strInvokePrams = "";
            for (int i = 0; i < typesList.Length; i++)
            {
                strPrams += (typesList[i] + " p" + (i + 1));
                strInvokePrams += ("p" + (i + 1));
                if (i < typesList.Length - 1)
                {
                    strPrams += (",");
                    strInvokePrams += (", ");
                }
            }
            startMethod = $@"
    public virtual void Start(string stepName, {strPrams})
    {{
    }}
";
            stringWriter.Write(startMethod);
        }

        string starMethods = stringWriter.ToString();
        stringWriter.Dispose();
        return starMethods;
    }
    private static string CreateDoNextMethodsStr(Dictionary<string, int> paramsTypeMap)
    {
        string doNextMethod = $@"
    public void DoNext(string stepName)
    {{
        string nextStepName = GetNextStepName(stepName);
        Start(nextStepName);
    }}
";
        StringWriter stringWriter = new StringWriter();
        stringWriter.Write(doNextMethod);
        foreach (KeyValuePair<string, int> pairs in paramsTypeMap)
        {
            string[] typesList = pairs.Key.Split(',');
            string strPrams = "";
            string strInvokePrams = "";
            for (int i = 0; i < typesList.Length; i++)
            {
                strPrams += (typesList[i] + " p" + (i + 1));
                strInvokePrams += ("p" + (i + 1));
                if (i < typesList.Length - 1)
                {
                    strPrams += (",");
                    strInvokePrams += (", ");
                }
            }
            doNextMethod = $@"
    public void DoNext(string stepName, {strPrams})
    {{
        string nextStepName = GetNextStepName(stepName);
        Start(nextStepName, {strInvokePrams});
    }}
";
            stringWriter.Write(doNextMethod);
        }

        string doNextMethods = stringWriter.ToString();
        stringWriter.Dispose();
        return doNextMethods;
    }
    private static string CreateNotifyParentDoNextMethodStr(Dictionary<string, int> paramsTypeMap)
    {
        StringWriter stringWriter = new StringWriter();
        foreach (KeyValuePair<string, int> pairs in paramsTypeMap)
        {
            string[] typesList = pairs.Key.Split(',');
            string conditionInfo = "ParentTab.DoNext(Name, ";
            for(int i = 1; i <= typesList.Length; i++)
            {
                conditionInfo += "_result" + pairs.Value + ".Item" + i;
                if (i != typesList.Length)
                {
                    conditionInfo += ", ";
                }
                else
                {
                    conditionInfo += ");";
                }
            }
            string condition = $@"
            else if (_resultIndex == {pairs.Value})
            {{
                {conditionInfo}
                return;
            }}
";
            stringWriter.WriteLine(condition);
        }

        string notifyParentDoNextNoConvertInfo = stringWriter.ToString();
        stringWriter.Dispose();

        string notifyParentDoNextNoConvert = $@"
    public void NotifyParentDoNextNoConvert()
    {{
        if (ParentTab == null)
        {{
            return;
        }}
        if (_resultIndex == -1)
        {{
            ParentTab.DoNext(Name);
            return;
        }}
        {notifyParentDoNextNoConvertInfo}
    }}
";


        return notifyParentDoNextNoConvert;
    }
    private static string GetNextStepName(string stepName)
    {
        ValueTuple<int, int> a = (1, 0);


        string result;
        char lastChar = stepName[stepName.Length - 1];
        if (lastChar >= '0' && lastChar <= '9')
        {
            int num = Convert.ToInt32(lastChar);
            char[] stepNameArray = stepName.ToCharArray();
            if (num == 9)
            {
                char[] nextStepNameArray = new char[stepName.Length + 1];
                Array.Copy(stepNameArray, nextStepNameArray, stepName.Length - 1);
                nextStepNameArray[stepName.Length - 1] = '1';
                nextStepNameArray[stepName.Length] = '0';
                result = (new string(nextStepNameArray));
            }
            else
            {
                char[] nextStepNameArray = new char[stepName.Length];
                Array.Copy(stepNameArray, nextStepNameArray, stepName.Length - 1);
                nextStepNameArray[stepName.Length - 1] = Convert.ToChar(num + 1);
                result = new string(nextStepNameArray);
            }
        }
        else
        {
            result = (stepName + "1");
        }
        return result;
    }
    private static string GetPreStepName(string stepName)
    {
        string result = "";
        char lastChar = stepName[stepName.Length - 1];
        if (lastChar >= '1' && lastChar <= '9')
        {
            int num = Convert.ToInt32(lastChar);
            char[] stepNameArray = stepName.ToCharArray();
            if (num == 9)
            {
                char[] nextStepNameArray = new char[stepName.Length];
                Array.Copy(stepNameArray, nextStepNameArray, stepName.Length - 1);
                nextStepNameArray[stepName.Length - 1] = Convert.ToChar(num - 1);
                result = (new string(nextStepNameArray));
            }
        }
        return result;
    }
    private static string CreateNextNameStr(MethodInfo[] methods)
    {
        string nextNameInfoStr = "";
        Dictionary<string, string> nameMap = new Dictionary<string, string>();
        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            Type returnType = method.ReturnType;
            if (returnType == typeof(void))
            {
                string preName = GetPreStepName(method.Name);
                string nextName = GetNextStepName(method.Name);
                if (preName != "" && !nameMap.ContainsKey(preName))
                {
                    nextNameInfoStr += ($"{{\"{preName}\", \"{method.Name}\"}}");
                    nameMap.Add(preName, method.Name);
                    if (i != methods.Length - 1 || !nameMap.ContainsKey(nextName))
                    {
                        nextNameInfoStr += ", ";
                    }
                }
                if (!nameMap.ContainsKey(nextName))
                {
                    nextNameInfoStr += ($"{{\"{method.Name}\", \"{nextName}\"}}");
                    nameMap.Add(method.Name, nextName);
                    if (i != methods.Length - 1)
                    {
                        nextNameInfoStr += ", ";
                    }
                }
            }
        }
        StringWriter stringWriter = new StringWriter();
        stringWriter.WriteLine($"   static Dictionary<string, string> nextNameFindMap = new Dictionary<string, string>(){{{nextNameInfoStr}}};");
        string nextNameStr = stringWriter.ToString();
        stringWriter.Dispose();
        return nextNameStr;
    }
    private static string CreateMethodListStr(Dictionary<string, int> singleParamsTypeMap)
    {
        StringWriter stringWriter = new StringWriter();
        stringWriter.WriteLine($"   static List<Action> method0List = new List<Action>();");
        foreach (KeyValuePair<string, int> pairs in singleParamsTypeMap)
        {
            stringWriter.WriteLine($"   static List<Action<{pairs.Key}>> method{pairs.Value}List = new List<Action<{pairs.Key}>>();");
        }
        string result = stringWriter.ToString();
        stringWriter.Dispose();
        return result;
    }
    private static string CreateMethodFindStr(Dictionary<string, int> singleParamsTypeMap, Dictionary<string, string> methodTypeMap, MethodInfo[] methods, out string compileStr)
    {
        StringWriter stringWriter = new StringWriter();
        string methodFindInfoStr = "";
        Dictionary<int, int> methodMap = new Dictionary<int, int>();
        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            Type returnType = method.ReturnType;
            if (returnType == typeof(void))
            {
                int paramsTypeIndex;
                string mapName;
                string paramsTypeStr;
                if (methodTypeMap.TryGetValue(method.Name, out paramsTypeStr))
                {
                    if (singleParamsTypeMap.TryGetValue(paramsTypeStr, out paramsTypeIndex))
                    {
                    }
                    else
                    {
                        paramsTypeIndex = 0;
                    }
                }
                else
                {
                    paramsTypeIndex = 0;
                }
                int mothodIndex;
                if (methodMap.TryGetValue(paramsTypeIndex, out mothodIndex))
                {

                }
                else
                {
                    mothodIndex = 0;
                }
                methodFindInfoStr += ($"{{\"{method.Name}\", {mothodIndex}}}");
                if (i != methods.Length - 1)
                {
                    methodFindInfoStr += ", ";
                }
                mothodIndex++;
                methodMap[paramsTypeIndex] = mothodIndex;
                mapName = "method" + paramsTypeIndex + "List";

                stringWriter.WriteLine($"   {mapName}.Add(this.{method.Name});");
            }
        }
        string compileInfoStr = stringWriter.ToString();
        stringWriter.Dispose();

        StringWriter stringWriter2 = new StringWriter();
        stringWriter2.WriteLine($"   static Dictionary<string, int> methodFindMap = new Dictionary<string, int>(){{{methodFindInfoStr}}};");
        string methodFindStr = stringWriter2.ToString();
        stringWriter2.Dispose();

        compileStr = $@"
    protected override void Compile()
    {{
    {compileInfoStr}
    }}
";
        return methodFindStr;

    }
    private static void CompileSingle(Type type, Dictionary<string, int> paramsTypeMap)
    {
        Dictionary<string, int> singleParamsTypeMap = new Dictionary<string, int>();
        Dictionary<string, string> methodTypeMap = new Dictionary<string, string>();
        var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        int index = 0;
        foreach (var method in methods)
        {
            ParameterInfo[] parameters = method.GetParameters();
            Type returnType = method.ReturnType;
            if (parameters.Length > 0 && returnType == typeof(void))
            {
                string typeKey = "";
                index++;
                for (int i = 0; i < parameters.Length; i++)
                {
                    string strType = parameters[i].ParameterType.ToString();
                    typeKey += strType;
                    if (i != parameters.Length - 1)
                    {
                        typeKey += ", ";
                    }
                }
                if (!paramsTypeMap.ContainsKey(typeKey))
                {
                    paramsTypeMap.Add(typeKey, index);
                }
                if (!singleParamsTypeMap.ContainsKey(typeKey))
                {
                    singleParamsTypeMap.Add(typeKey, index);
                }
                if (!methodTypeMap.ContainsKey(method.Name))
                {
                    methodTypeMap.Add(method.Name, typeKey);
                }
            }
        }


        string nextNameStr = CreateNextNameStr(methods);
        string methodListStr = CreateMethodListStr(singleParamsTypeMap);
        string compileStr = "";
        string methodFindStr = CreateMethodFindStr(singleParamsTypeMap, methodTypeMap, methods, out compileStr);
        string methodFind = $@"
    private bool MethodFind(string stepName)
    {{
        if(methodFindMap.ContainsKey(stepName))
        {{
            return true;
        }}
        return false;
    }}
";

        string autoStop = $@"
    public override bool AutoStop(string updateName, string eventName)
    {{
        return !methodFindMap.ContainsKey(updateName) && !methodFindMap.ContainsKey(eventName);
    }}
";
        string initstall = $@"
    public override void Initstall()
    {{
        TabStep tabStep = new TabStep(this, ""root"", true);
        tabStep.IsAutoStop = AutoStop(""update"", ""event"");
        this.MainStep = tabStep;
    }}
";

        string nextStepName = $@"
    protected override string GetNextStepName(string stepName)
    {{
        string name = """";
        if (!nextNameFindMap.TryGetValue(stepName, out name))
        {{
            name = base.GetNextStepName(stepName);
        }}
        return name;
    }}
";
        
    string notifyParentDoNext = $@"
    protected override void NotifyParentDoNext()
    {{
        base.NotifyParentDoNextNoConvert();
    }}
";

        string methodName = "method0List";
        string startMethod = $@"
    public override void Start(string stepName)
    {{
        int insertIndex = 0;
        if (methodFindMap.TryGetValue(stepName, out insertIndex))
        {{
            TabStep tabStep = CreateStep(stepName, true);
            Action action = {methodName}[insertIndex];
            action.Invoke();
            if (tabStep.IsAutoStop)
            {{
                tabStep.IsStop = true;
                DoNext(stepName);
            }}
        }}
        else
        {{
            OnStartError();
        }}
    }}
";
        StringWriter stringWriter = new StringWriter();
        stringWriter.Write(startMethod);
        foreach (KeyValuePair<string, int> pairs in singleParamsTypeMap)
        {
            methodName = "method" + pairs.Value + "List";
            string[] types = pairs.Key.Split(',');
            string strPrams = "";
            string strInvokePrams = "";
            for (int i = 0; i < types.Length; i++)
            {
                strPrams += (types[i] + " p" + (i + 1));
                strInvokePrams += ("p" + (i + 1));
                if (i < types.Length - 1)
                {
                    strPrams += (",");
                    strInvokePrams += (", ");
                }
            }
            startMethod = $@"
    public override void Start(string stepName, {strPrams})
    {{
        int insertIndex = 0;
        if (methodFindMap.TryGetValue(stepName, out insertIndex))
        {{
            TabStep tabStep = CreateStep(stepName, true);            
            Action<{pairs.Key}> action = {methodName}[insertIndex];
            action.Invoke({strInvokePrams});
            {{
                tabStep.IsStop = true;
                DoNext(stepName);
            }}
        }}
        else
        {{
            OnStartError();
        }}
    }}
";
            stringWriter.Write(startMethod);
        }
        string startMethods = stringWriter.ToString();
        stringWriter.Dispose();

        string className = type.Name;
        string fileContent = $@"
using System;
using System.Collections.Generic;

public partial class {className} : Tab
{{
{nextNameStr}
{methodFindStr}
{methodListStr}
{compileStr}
{initstall}
{notifyParentDoNext}
{nextStepName}
{autoStop}
{methodFind}
{startMethods}
}}";
        File.WriteAllText("Assets/Scripts/TabMachineWrap/" + className+"_warp.cs", fileContent);
        //FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        //for (int i = 0; i < fields.Length; i++)
        //{
        //    FieldInfo field = fields[i];
        //    if (field.Name == "_outPut" && field.FieldType.IsGenericType)
        //    {
        //        Type[] tupleElementTypes = field.FieldType.GetGenericArguments();
        //    }
        //}
    }
}