using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace CompilerLib
{

    public class Compiler
    {
        TypeBuilder prog;
        FieldBuilder charT;
        char[] Operators = { '0', '>', '<', 'V', 'A', '{', '}', '+', '-', '$', '#' };
        Dictionary<char, char[,]> MethodsSource;
        char[,] MainSource;
        MethodBuilder MainMethod; //This is not an entry point: this is compiled main source.
        MethodBuilder ActionPerfomer;
        MethodBuilder initChar;
        Dictionary<char, MethodBuilder> MethodsList;
        List<char> Names;
        int NumberOfMethods;//except main method
        public Compiler(char[,] main, Dictionary<char, char[,]> methodsSourceWithNames)
        {
            MainSource = (char[,])(main.Clone());
            MethodsSource = methodsSourceWithNames;
            Names = methodsSourceWithNames.Keys.ToList();
            Names.Sort();
        }
        private void CreateInitChar()
        {
            initChar = prog.DefineMethod("InitChars", MethodAttributes.Private | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
            ILGenerator ILinit = initChar.GetILGenerator();
            //создаем цикл
            LocalBuilder i = ILinit.DeclareLocal(typeof(int));
            LocalBuilder j = ILinit.DeclareLocal(typeof(int));
            LocalBuilder tmpArr = ILinit.DeclareLocal(typeof(byte).MakeArrayType(1));
            LocalBuilder ascii_Encoding = ILinit.DeclareLocal(typeof(System.Text.Encoding));
            ILinit.Emit(OpCodes.Ldc_I4, 16);
            ILinit.Emit(OpCodes.Ldc_I4, 16);
            ILinit.Emit(OpCodes.Newobj, typeof(char).MakeArrayType(2).GetConstructor(new Type[] { typeof(int), typeof(int) }));
            ILinit.Emit(OpCodes.Stsfld, charT);
            Label test1 = ILinit.DefineLabel();
            Label end1 = ILinit.DefineLabel();
            ILinit.Emit(OpCodes.Call, typeof(System.Text.Encoding).GetMethod("get_ASCII"));
            ILinit.Emit(OpCodes.Stloc, ascii_Encoding);
            // outside loop
            ILinit.Emit(OpCodes.Ldc_I4, 0);
            ILinit.Emit(OpCodes.Stloc, i);
            ILinit.MarkLabel(test1);
            ILinit.Emit(OpCodes.Ldloc, i);
            ILinit.Emit(OpCodes.Ldc_I4, 15);
            ILinit.Emit(OpCodes.Bgt, end1);
            //inside loop
            ILinit.Emit(OpCodes.Ldc_I4, 0);
            ILinit.Emit(OpCodes.Stloc, j);
            Label test2 = ILinit.DefineLabel();
            Label end2 = ILinit.DefineLabel();
            ILinit.MarkLabel(test2);
            ILinit.Emit(OpCodes.Ldloc, j);
            ILinit.Emit(OpCodes.Ldc_I4, 15);
            ILinit.Emit(OpCodes.Bgt, end2);
            //declaring of the body
            ILinit.Emit(OpCodes.Ldsfld, charT);
            ILinit.Emit(OpCodes.Ldloc, i);
            ILinit.Emit(OpCodes.Ldloc, j);
            ILinit.Emit(OpCodes.Ldloc, ascii_Encoding);
            ILinit.Emit(OpCodes.Ldc_I4, 1);
            ILinit.Emit(OpCodes.Newarr, typeof(byte));
            ILinit.Emit(OpCodes.Stloc, tmpArr);
            ILinit.Emit(OpCodes.Ldloc, tmpArr);
            ILinit.Emit(OpCodes.Ldc_I4, 0);
            ILinit.Emit(OpCodes.Ldloc, i);
            ILinit.Emit(OpCodes.Ldc_I4, 16);
            ILinit.Emit(OpCodes.Mul);
            ILinit.Emit(OpCodes.Ldloc, j);
            ILinit.Emit(OpCodes.Add);
            ILinit.Emit(OpCodes.Conv_U1);
            ILinit.Emit(OpCodes.Stelem_I1);
            ILinit.Emit(OpCodes.Ldloc, tmpArr);
            ILinit.EmitCall(OpCodes.Callvirt, typeof(Encoding).GetMethods().First(s => { return s.Name == "GetString"; }),
                new Type[] { typeof(byte).MakeArrayType(1) });
            ILinit.Emit(OpCodes.Ldc_I4, 0);
            ILinit.EmitCall(OpCodes.Callvirt, typeof(string).GetMethod("get_Chars"), new Type[] { typeof(int) });
            ILinit.EmitCall(OpCodes.Call, typeof(char).MakeArrayType(2).GetMethod("Set"), new Type[] { typeof(int), typeof(int), typeof(char) });
            //incrementing of the second iterator
            ILinit.Emit(OpCodes.Ldloc, j);
            ILinit.Emit(OpCodes.Ldc_I4, 1);
            ILinit.Emit(OpCodes.Add);
            ILinit.Emit(OpCodes.Stloc, j);
            ILinit.Emit(OpCodes.Br, test2);
            //end of inside loop
            ILinit.MarkLabel(end2);
            //incrementing of the first iterator
            ILinit.Emit(OpCodes.Ldloc, i);
            ILinit.Emit(OpCodes.Ldc_I4, 1);
            ILinit.Emit(OpCodes.Add);
            ILinit.Emit(OpCodes.Stloc, i);
            ILinit.Emit(OpCodes.Br, test1);
            //End of method
            ILinit.MarkLabel(end1);
            ILinit.Emit(OpCodes.Ret);
        }
        private void CreateActionPerfomer()
        {
            ActionPerfomer = prog.DefineMethod("ActionPerfoming", MethodAttributes.Static | MethodAttributes.Private, typeof(void), new Type[] { typeof(int).MakeArrayType(2) });
            ILGenerator ILAct = ActionPerfomer.GetILGenerator();
            LocalBuilder i = ILAct.DeclareLocal(typeof(int));
            LocalBuilder j = ILAct.DeclareLocal(typeof(int));
            LocalBuilder direction = ILAct.DeclareLocal(typeof(int));// 0 - right, 1 - bottom, 2 - left, 3 - up;
            ILAct.Emit(OpCodes.Ldc_I4_0);
            ILAct.Emit(OpCodes.Stloc, i);
            ILAct.Emit(OpCodes.Ldc_I4_0);
            ILAct.Emit(OpCodes.Stloc, j);
            ILAct.Emit(OpCodes.Ldc_I4_0);
            ILAct.Emit(OpCodes.Stloc, direction);
            Label[] JumpTable = new Label[Operators.Length + NumberOfMethods];
            for (int k = 0; k < JumpTable.Length; k++) JumpTable[k] = ILAct.DefineLabel();
            Label ReSwitch = ILAct.DefineLabel();
            Label EndSwitch = ILAct.DefineLabel();
            Label EndOfMethod = ILAct.DefineLabel();
            ILAct.MarkLabel(ReSwitch);
            ILAct.Emit(OpCodes.Ldarg_0);
            ILAct.Emit(OpCodes.Ldloc, i);
            ILAct.Emit(OpCodes.Ldloc, j);
            ILAct.EmitCall(OpCodes.Call, typeof(int).MakeArrayType(2).GetMethod("Get"), new Type[] { typeof(int), typeof(int) });
            //Now we creating switch operator
            ILAct.Emit(OpCodes.Switch, JumpTable);
            ILAct.Emit(OpCodes.Br, EndSwitch);
            //case 0: we change direction
            {
                ILAct.MarkLabel(JumpTable[0]);
                // We create little jump table
                Label[] lJT = new Label[4];
                for (int k = 0; k < 4; k++) lJT[k] = ILAct.DefineLabel();
                ILAct.Emit(OpCodes.Ldloc, direction);
                ILAct.Emit(OpCodes.Switch, lJT);
                //we start to move left
                ILAct.MarkLabel(lJT[0]);
                CreateActionPerfomer_Inc_Dec(false, j, ILAct);
                ILAct.Emit(OpCodes.Ldc_I4_2);
                ILAct.Emit(OpCodes.Stloc, direction);
                ILAct.Emit(OpCodes.Br, EndSwitch);
                //we start to move up
                ILAct.MarkLabel(lJT[1]);
                CreateActionPerfomer_Inc_Dec(false, i, ILAct);
                ILAct.Emit(OpCodes.Ldc_I4_3);
                ILAct.Emit(OpCodes.Stloc, direction);
                ILAct.Emit(OpCodes.Br, EndSwitch);
                //we start to move right
                ILAct.MarkLabel(lJT[2]);
                CreateActionPerfomer_Inc_Dec(true, j, ILAct);
                ILAct.Emit(OpCodes.Ldc_I4_0);
                ILAct.Emit(OpCodes.Stloc, direction);
                ILAct.Emit(OpCodes.Br, EndSwitch);
                //we start to move down
                ILAct.MarkLabel(lJT[3]);
                CreateActionPerfomer_Inc_Dec(true, i, ILAct);
                ILAct.Emit(OpCodes.Ldc_I4_1);
                ILAct.Emit(OpCodes.Stloc, direction);
                ILAct.Emit(OpCodes.Br, EndSwitch);
            }
            //case 1: we move right
            ILAct.MarkLabel(JumpTable[1]);
            CreateActionPerformer_ChangingCommand(ILAct, i, j, 2);//change command in this position
            CreateActionPerfomer_Inc_Dec(true, j, ILAct);//incrementig column index
            ILAct.Emit(OpCodes.Ldc_I4_0);//changing of direction
            ILAct.Emit(OpCodes.Stloc, direction);
            ILAct.Emit(OpCodes.Br, EndSwitch);

            //case 2: we move left
            ILAct.MarkLabel(JumpTable[2]);
            CreateActionPerformer_ChangingCommand(ILAct, i, j, 1);//change command in this position
            CreateActionPerfomer_Inc_Dec(false, j, ILAct);//decrementig column index
            ILAct.Emit(OpCodes.Ldc_I4_2);//changing of direction
            ILAct.Emit(OpCodes.Stloc, direction);
            ILAct.Emit(OpCodes.Br, EndSwitch);

            //case 3: we move down
            ILAct.MarkLabel(JumpTable[3]);
            CreateActionPerformer_ChangingCommand(ILAct, i, j, 4);//change command in this position
            CreateActionPerfomer_Inc_Dec(true, i, ILAct);//incrementig row index
            ILAct.Emit(OpCodes.Ldc_I4_1);//changing of direction
            ILAct.Emit(OpCodes.Stloc, direction);
            ILAct.Emit(OpCodes.Br, EndSwitch);

            //case 4: we move up
            ILAct.MarkLabel(JumpTable[4]);
            CreateActionPerformer_ChangingCommand(ILAct, i, j, 3);//change command in this position
            CreateActionPerfomer_Inc_Dec(false, i, ILAct);//decrementig row index
            ILAct.Emit(OpCodes.Ldc_I4_3);//changing of direction
            ILAct.Emit(OpCodes.Stloc, direction);
            ILAct.Emit(OpCodes.Br, EndSwitch);

            //case 5: we print symbol and move left
            ILAct.MarkLabel(JumpTable[5]);
            CreateActionPerfomer_Print(ILAct, i, j);//change command in this position
            CreateActionPerfomer_Inc_Dec(false, j, ILAct);//decrementig column index
            ILAct.Emit(OpCodes.Ldc_I4_2);//changing of direction
            ILAct.Emit(OpCodes.Stloc, direction);
            ILAct.Emit(OpCodes.Br, EndSwitch);

            //case 6: we print symbol and move right
            ILAct.MarkLabel(JumpTable[6]);
            CreateActionPerfomer_Print(ILAct, i, j);//change command in this position
            CreateActionPerfomer_Inc_Dec(true, j, ILAct);//decrementig column index
            ILAct.Emit(OpCodes.Ldc_I4_0);//changing of direction
            ILAct.Emit(OpCodes.Stloc, direction);
            ILAct.Emit(OpCodes.Br, EndSwitch);

            //case 7: we print symbol and move up
            ILAct.MarkLabel(JumpTable[7]);
            CreateActionPerfomer_Print(ILAct, i, j);//change command in this position
            CreateActionPerfomer_Inc_Dec(false, i, ILAct);//decrementig row index
            ILAct.Emit(OpCodes.Ldc_I4_3);//changing of direction
            ILAct.Emit(OpCodes.Stloc, direction);
            ILAct.Emit(OpCodes.Br, EndSwitch);

            //case 8: we print symbol and move down
            ILAct.MarkLabel(JumpTable[8]);
            CreateActionPerfomer_Print(ILAct, i, j);//change command in this position
            CreateActionPerfomer_Inc_Dec(true, i, ILAct);//incrementig row index
            ILAct.Emit(OpCodes.Ldc_I4_1);//changing of direction
            ILAct.Emit(OpCodes.Stloc, direction);
            ILAct.Emit(OpCodes.Br, EndSwitch);

            //case 9: we print symbol and move to 0,0
            ILAct.MarkLabel(JumpTable[9]);
            CreateActionPerfomer_Print(ILAct, i, j);//change command in this position
            //changing of indexes
            ILAct.Emit(OpCodes.Ldc_I4_0); ILAct.Emit(OpCodes.Stloc, i);
            ILAct.Emit(OpCodes.Ldc_I4_0); ILAct.Emit(OpCodes.Stloc, j);
            ILAct.Emit(OpCodes.Br, EndSwitch);

            //case 10: we exit from method
            ILAct.MarkLabel(JumpTable[10]);
            ILAct.Emit(OpCodes.Br, EndOfMethod);

            // there is calling another methods
            for (int k = Operators.Length; k < JumpTable.Length; k++)
            {
                ILAct.MarkLabel(JumpTable[k]);
                ILAct.EmitCall(OpCodes.Call, MethodsList[Names[k - Operators.Length]], Type.EmptyTypes);
                Label[] lJT = new Label[4];
                for (int m = 0; m < 4; m++) lJT[m] = ILAct.DefineLabel();
                ILAct.Emit(OpCodes.Ldloc, direction);
                ILAct.Emit(OpCodes.Switch, lJT);
                //we continue to moving right
                ILAct.MarkLabel(lJT[0]);
                CreateActionPerfomer_Inc_Dec(true, j, ILAct);
                ILAct.Emit(OpCodes.Br, EndSwitch);
                //we continue to moving down
                ILAct.MarkLabel(lJT[1]);
                CreateActionPerfomer_Inc_Dec(true, i, ILAct);
                ILAct.Emit(OpCodes.Br, EndSwitch);
                //we continue to moving left
                ILAct.MarkLabel(lJT[2]);
                CreateActionPerfomer_Inc_Dec(false, j, ILAct);
                ILAct.Emit(OpCodes.Br, EndSwitch);
                //we continue to moving up
                ILAct.MarkLabel(lJT[3]);
                CreateActionPerfomer_Inc_Dec(false, i, ILAct);
                ILAct.Emit(OpCodes.Br, EndSwitch);
            }
            ILAct.MarkLabel(EndSwitch);
            /* We create this 
             * if (i < 0) i = 16 + i;
             *if (j < 0) j = 16 + j;
             *if (i >= 16) i = i % 16;
             *if (j >= 16) j = j % 16;
             */
            CreateActionPerformer_CheckIndex(ILAct, i);
            CreateActionPerformer_CheckIndex(ILAct, j);
            ILAct.Emit(OpCodes.Br, ReSwitch);
            ILAct.MarkLabel(EndOfMethod);
            ILAct.Emit(OpCodes.Ret);
        }
        private void CreateActionPerfomer_Inc_Dec(bool increase, LocalBuilder ind, ILGenerator IL) // create instructions ind++ or ind--
        {
            if (IL == null || ind == null) throw new ArgumentNullException("Error in CreateActionPerfomer method");
            IL.Emit(OpCodes.Ldloc, ind);
            IL.Emit(OpCodes.Ldc_I4_1);
            if (increase) IL.Emit(OpCodes.Add);
            else IL.Emit(OpCodes.Sub);
            IL.Emit(OpCodes.Stloc, ind);
        }
        private void CreateActionPerfomer_Print(ILGenerator IL, LocalBuilder ind1, LocalBuilder ind2) // create console printing
        {
            if (IL == null || ind1 == null || ind2 == null) throw new ArgumentNullException("Error in CreateActionPerfomer method");
            IL.Emit(OpCodes.Ldsfld, charT); // We load a field
            IL.Emit(OpCodes.Ldloc, ind1);
            IL.Emit(OpCodes.Ldloc, ind2);
            IL.EmitCall(OpCodes.Call, typeof(char).MakeArrayType(2).GetMethod("Get"), new Type[] { typeof(int), typeof(int) });
            IL.Emit(OpCodes.Call, typeof(System.Console).GetMethod("Write", new System.Type[] { typeof(char) }));
        }
        private void CreateActionPerformer_ChangingCommand(ILGenerator IL, LocalBuilder ind1, LocalBuilder ind2, int newValue) // changes command in Table[ind1,in2] to new value
        {
            if (IL == null || ind1 == null || ind2 == null) throw new ArgumentNullException("Error in CreateActionPerfomer method");
            IL.Emit(OpCodes.Ldarg_0);
            IL.Emit(OpCodes.Ldloc, ind1);
            IL.Emit(OpCodes.Ldloc, ind2);
            switch (newValue)
            {
                case 0: IL.Emit(OpCodes.Ldc_I4_0); break;
                case 1: IL.Emit(OpCodes.Ldc_I4_1); break;
                case 2: IL.Emit(OpCodes.Ldc_I4_2); break;
                case 3: IL.Emit(OpCodes.Ldc_I4_3); break;
                case 4: IL.Emit(OpCodes.Ldc_I4_4); break;
                case 5: IL.Emit(OpCodes.Ldc_I4_5); break;
                case 6: IL.Emit(OpCodes.Ldc_I4_6); break;
                case 7: IL.Emit(OpCodes.Ldc_I4_7); break;
                case 8: IL.Emit(OpCodes.Ldc_I4_8); break;
                default: IL.Emit(OpCodes.Ldc_I4, newValue); break;
            }
            IL.EmitCall(OpCodes.Call, typeof(int).MakeArrayType(2).GetMethod("Set"), new Type[] { typeof(int), typeof(int), typeof(int) });
        }
        private void CreateActionPerformer_CheckIndex(ILGenerator IL, LocalBuilder ind)//check ind in [0;15]
        {
            if (IL == null || ind == null) throw new ArgumentNullException("Error in CreateActionPerfomer method");
            Label jmp1 = IL.DefineLabel();
            IL.Emit(OpCodes.Ldloc, ind);
            IL.Emit(OpCodes.Ldc_I4_0);
            IL.Emit(OpCodes.Clt);
            IL.Emit(OpCodes.Brfalse, jmp1);
            IL.Emit(OpCodes.Ldloc, ind);
            IL.Emit(OpCodes.Ldc_I4, 16);
            IL.Emit(OpCodes.Add);
            IL.Emit(OpCodes.Stloc, ind);
            IL.MarkLabel(jmp1);
            Label jmp2 = IL.DefineLabel();
            IL.Emit(OpCodes.Ldloc, ind);
            IL.Emit(OpCodes.Ldc_I4, 16);
            IL.Emit(OpCodes.Clt);
            IL.Emit(OpCodes.Brtrue, jmp2);
            IL.Emit(OpCodes.Ldloc, ind);
            IL.Emit(OpCodes.Ldc_I4, 16);
            IL.Emit(OpCodes.Rem);
            IL.Emit(OpCodes.Stloc, ind);
            IL.MarkLabel(jmp2);
        }

        private void InitMethodsInformation()
        {
            Names.Sort();
            MethodsList = new Dictionary<char, MethodBuilder>();
            for (int i = 0; i < Names.Count; i++)
            {
                if (MethodsList.ContainsKey(Names[i]))
                    throw new Exception("Incorrect method names list.");
                MethodBuilder mb = prog.DefineMethod(Names[i].ToString(), MethodAttributes.Static | MethodAttributes.Private);
                MethodsList.Add(Names[i], mb);
            }
            NumberOfMethods = Names.Count;
            MainMethod = prog.DefineMethod("Main", MethodAttributes.Static | MethodAttributes.Private);
        }
        private bool IsValidMethodName(char name)
        {
            return !Array.Exists(Operators, (char a) => { return a == name; }) && ('A' < name && 'Z' >= name || 'a' <= name && 'z' >= name);
        }
        private void GenerateMethod(char[,] Source, MethodBuilder mb)
        {
            for (int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++)
                {
                    char Letter = Source[i, j];

                }
            ILGenerator IL = mb.GetILGenerator();
            LocalBuilder Table = IL.DeclareLocal(typeof(int).MakeArrayType(2));//we create list of commands
            IL.Emit(OpCodes.Ldc_I4, 16);
            IL.Emit(OpCodes.Ldc_I4, 16);
            IL.Emit(OpCodes.Newobj, typeof(int).MakeArrayType(2).GetConstructor(new Type[] { typeof(int), typeof(int) }));
            IL.Emit(OpCodes.Stloc, Table);
            //We fill list of commands
            for (int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++)
                    if (Source[i, j] != '0')
                    {
                        IL.Emit(OpCodes.Ldloc, Table);
                        IL.Emit(OpCodes.Ldc_I4, i);
                        IL.Emit(OpCodes.Ldc_I4, j);
                        int code;
                        switch (Source[i, j])
                        {
                            case '0': code = 0; break;
                            case '>': code = 1; break;
                            case '<': code = 2; break;
                            case 'V': code = 3; break;
                            case 'A': code = 4; break;
                            case '{': code = 5; break;
                            case '}': code = 6; break;
                            case '+': code = 7; break;
                            case '-': code = 8; break;
                            case '$': code = 9; break;
                            case '#': code = 10; break;
                            default: code = Names.IndexOf(Source[i, j]) + Operators.Length; break; //All methods calling codes start from  11
                        }
                        IL.Emit(OpCodes.Ldc_I4, code);
                        IL.EmitCall(OpCodes.Call, typeof(int).MakeArrayType(2).GetMethod("Set"), new Type[] { typeof(int), typeof(int), typeof(int) });
                    }
            IL.Emit(OpCodes.Ldloc, Table);
            IL.EmitCall(OpCodes.Call, this.ActionPerfomer, new Type[] { typeof(int).MakeArrayType(2) });
            IL.Emit(OpCodes.Ret);
        }
        public Tuple<int,CompileErrors> GenerateCode(string ModuleName)
        {
            try
            {
                //We check errors in source code
                CompileErrors errors = new CompileErrors();
                errors.Criticals = CompileErrors.CheckFatalErrors(MainSource, MethodsSource);
                errors.NonCriticals = CompileErrors.CheckRecursions(MainSource, MethodsSource);
                if (errors.Criticals.Count > 0)
                    return new Tuple<int, CompileErrors>(1, errors);
                if (!Directory.Exists(Path.GetDirectoryName(ModuleName)))
                    Directory.CreateDirectory(Path.GetDirectoryName(ModuleName));
                Directory.SetCurrentDirectory(Path.GetDirectoryName(ModuleName));
                AssemblyName name = new AssemblyName(Path.GetFileName(ModuleName));
                AssemblyBuilder AB = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save);
                ModuleBuilder MB = AB.DefineDynamicModule(Path.GetFileNameWithoutExtension(ModuleName) + ".exe");
                prog = MB.DefineType("Program");
                charT = prog.DefineField("CharTable", typeof(char).MakeArrayType(2), FieldAttributes.Private | FieldAttributes.Static);
                CreateInitChar();
                InitMethodsInformation();
                CreateActionPerfomer();
                GenerateMethod(MainSource, MainMethod);
                for (int i = 0; i < Names.Count; i++)
                    GenerateMethod(MethodsSource[Names[i]], MethodsList[Names[i]]);
                MethodBuilder entryPoint = prog.DefineMethod("Entry", MethodAttributes.Public | MethodAttributes.Static);
                ILGenerator entryIL = entryPoint.GetILGenerator();
                entryIL.EmitCall(OpCodes.Call, initChar, Type.EmptyTypes);
                entryIL.EmitCall(OpCodes.Call, MainMethod, Type.EmptyTypes);
                entryIL.EmitCall(OpCodes.Call, typeof(Console).GetMethod("WriteLine", Type.EmptyTypes), Type.EmptyTypes);
                entryIL.EmitCall(OpCodes.Call, typeof(Console).GetMethod("ReadKey", Type.EmptyTypes), Type.EmptyTypes);
                entryIL.Emit(OpCodes.Pop);
                entryIL.Emit(OpCodes.Ret);
                prog.CreateType();
                MB.CreateGlobalFunctions();
                AB.SetEntryPoint(entryPoint);
                AB.Save(name.FullName);
                return new Tuple<int, CompileErrors>(0, errors);
            }
            catch
            {
                return new Tuple<int, CompileErrors>(2, null);
            }
        }
    }
}
