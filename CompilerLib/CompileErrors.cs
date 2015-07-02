using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilerLib
{
    public class CompileErrors
    {
        public List<Error> Criticals;
        public List<Error> NonCriticals;
        public CompileErrors()
        {
            Criticals = new List<Error>();
            NonCriticals = new List<Error>();
        }
        public static List<Error> CheckFatalErrors(char[,] main, Dictionary<char,char[,]> procedures)
        {
            List<Error> result = new List<Error>();
            char[] operators = { '0', '>', '<', 'V', 'A', '{', '}', '+', '-', '$', '#' };
            char[] directionalSymbols = {'>','<','V','A','{', '}', '+', '-','#'}; //Symbols which change direction or interrupt running.
            char ret = '#';
            bool hasReturn = false;
            for (int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++)
                    if (operators.Contains(main[i, j]) || procedures.Keys.Contains(main[i, j]))
                    {
                        if (ret == main[i, j])
                            hasReturn = true;
                    }
                    else
                        result.Add(new Error("Main", i, j, ErrorType.UnknownSymbol));
            if (!hasReturn)
                result.Add(new Error("Main", 0, 0, ErrorType.MissReturn));
            if(!directionalSymbols.Contains(main[0,0]))
                result.Add(new Error("Main", 0, 0, ErrorType.MissDirectionalSymbolInFirstCell));
            foreach(char a in procedures.Keys)
            {
                hasReturn = false;
                for (int i = 0; i < 16; i++)
                    for (int j = 0; j < 16; j++)
                        if (operators.Contains(procedures[a][i, j]) || procedures.Keys.Contains(procedures[a][i, j]))
                        {
                            if (ret == procedures[a][i, j])
                                hasReturn = true;
                        }
                        else
                            result.Add(new Error(a.ToString(), i, j, ErrorType.UnknownSymbol));
                if (!hasReturn)
                    result.Add(new Error(a.ToString(), 0, 0, ErrorType.MissReturn));
                if (!directionalSymbols.Contains(procedures[a][0, 0]))
                    result.Add(new Error(a.ToString(), 0, 0, ErrorType.MissDirectionalSymbolInFirstCell));
            }
            return result;
        }
        /// <summary>
        /// Checks procedures on recursions
        /// </summary>
        /// <param name="main">Source of main module of the program</param>
        /// <param name="procedures">All procedures in project</param>
        /// <returns>List of recursions and unused methods</returns>
        public static List<Error> CheckRecursions(char[,] main, Dictionary<char,char[,]> procedures)
        {
            List<char> calledMethods = new List<char>();
            Dictionary<char, bool> isUsed = new Dictionary<char, bool>(procedures.Count);
            foreach (char a in procedures.Keys)
                isUsed.Add(a, false);
            for (int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++)
                    if (procedures.ContainsKey(main[i, j]) && !calledMethods.Contains(main[i, j]))
                    {
                        calledMethods.Add(main[i, j]);
                        isUsed[main[i, j]] = true;
                    }
            List<Error> errors = new List<Error>();
            foreach(char a in calledMethods)
            {
                var currentMethod = new Tuple<char, char[,]>(a, procedures[a]);
                errors.AddRange(CheckMethodOnCalls("", currentMethod, procedures,isUsed));
            }
            for (int i = 0; i < errors.Count; i++)
                if (errors[i].Type == ErrorType.Recursion)
                    for (int j = i + 1; j < errors.Count; j++)
                        if (errors[i].Type == errors[j].Type && errors[i].Resource.ToString() == errors[j].Resource.ToString())
                            errors.RemoveAt(j--);
            foreach(char a in isUsed.Keys)
            {
                if (!isUsed[a])
                    errors.Add(new Error(a.ToString(), 0, 0, ErrorType.UnUsedProcedure));
            }
            return errors;
        }
        protected static List<Error> CheckMethodOnCalls(string previousCalls, Tuple<char,char[,]> procedure, Dictionary<char, char[,]> allProcedures, Dictionary<char,bool> isUsed)
        {
            previousCalls += procedure.Item1;
            List<Error> result = new List<Error>();
            for (int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++)
                {
                    char key = procedure.Item2[i, j];
                    if (allProcedures.ContainsKey(key))
                        if (previousCalls.Contains(key))
                        {
                            Error error = new Error(procedure.Item1.ToString(), i, j, ErrorType.Recursion);
                            error.Resource = previousCalls.Substring(previousCalls.IndexOf(key)) + key;
                            result.Add(error);
                        }
                        else
                        {
                            isUsed[key] = true;
                            result.AddRange(CheckMethodOnCalls(previousCalls, new Tuple<char, char[,]>(key, allProcedures[key]), allProcedures,isUsed));
                        }
                    
                }
            return result;
        }
    }
    public class Error
    {
        public string ModuleName;
        public int Row;
        public int Column;
        private ErrorType _type;
        public ErrorType Type
        {
            get { return _type; }
            set
            {
                LockCompile = (value != ErrorType.UnUsedProcedure && value != ErrorType.Recursion);
                _type = value;
            }
        }
        public bool LockCompile { get; protected set; }// This value is true if the error prevent compiling.
        public dynamic Resource;
        public Error()
        {
            ModuleName = string.Empty;
            Column = Row = 0;
        }
        public Error(string module, int row, int column, ErrorType type)
        {
            ModuleName = module;
            Row = row;
            Column = column;
            Type = type;
        }
    }
    public enum ErrorType { UnknownSymbol, MissReturn, MissDirectionalSymbolInFirstCell, UnUsedProcedure, Recursion}
}
