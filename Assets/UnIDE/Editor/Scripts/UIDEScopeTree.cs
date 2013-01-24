using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Text;
//using DDW;
//using DDW.Collections;
//using DDW.Enums;
//using DDW.Names;
//using Microsoft.CSharp;

/*
public class UIDEScopeTree:System.Object {
	public string characters;
	
    private static void PrintErrors(IEnumerable errors)
    {
        foreach (Parser.Error error in errors)
        {
            if(error.Token.ID == TokenID.Eof && error.Line == -1)
            {
                UnityEngine.Debug.Log(error.Message + "\nFile: " + error.FileName + "\n");
            }
            else
            {
                UnityEngine.Debug.Log(error.Message + " in token " + error.Token.ID
                    + "\nline: " + error.Line + ", column: " + error.Column
                    + "\nin file: " + error.FileName + "\n");
            }
        }
    }
    
    private static CompilationUnitNode ParseUnit(string fileName, List<DDW.Parser.Error> errors)
    {
        UnityEngine.Debug.Log("\nParsing " + fileName);
        FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        StreamReader sr = new StreamReader(fs, true);
        Lexer l = new Lexer(sr);
        TokenCollection toks = l.Lex();

        Parser p = null;
        CompilationUnitNode cu = null;

        p = new Parser(fileName);
		
        cu = p.Parse(toks, l.StringLiterals);

        if(p.Errors.Count != 0)
        {
            UnityEngine.Debug.Log("");
            PrintErrors(p.Errors);
            errors.AddRange(p.Errors);
            return null;
        }
        return cu;
    }
    
	public static void ParseStuff()
	{
		string filename = "Assets/UnIDE/Scripts/LocalGravity.cs";
		List<DDW.Parser.Error> errors = new List<DDW.Parser.Error>();
		CompilationUnitNode cu = ParseUnit(filename, errors);
		
		StringBuilder sb = new StringBuilder();
		cu.ToSource(sb);
		
		UnityEngine.Debug.Log(sb.ToString());
		foreach (NamespaceNode nnode in cu.Namespaces)
		{
			
			foreach (ClassNode cnode in nnode.Classes)
			{
				
				UnityEngine.Debug.Log("class name: " + cnode.Name.Identifier.ToString());
				foreach (FieldNode fnode in cnode.Fields)
				{
					sb = new StringBuilder();
					fnode.Type.ToSource(sb);
					sb.Append(" ");
					fnode.Names[0].Expressions.ToSource(sb);
					if (fnode.Value != null) {
						sb.Append(" = ");
						fnode.Value.ToSource(sb);
					}
					UnityEngine.Debug.Log(sb.ToString());
				}
				foreach (MethodNode mnode in cnode.Methods)
				{
					sb = new StringBuilder();
					mnode.Names.ToSource(sb);

					UnityEngine.Debug.Log("Line number of "+mnode.Names.ToString()+" starting: " + mnode.RelatedToken.Line.ToString());
					int firstLine = -1;
					//int lastLine = -1;
					
					UnityEngine.Debug.Log(mnode.Names.Count);
					
					foreach (StatementNode snode in mnode.StatementBlock.Statements)
					{
						if (firstLine == -1)
						{
							firstLine = snode.RelatedToken.Line;
						}
						StatementNode ifNode = (StatementNode)snode;
						
						if (ifNode != null) {
							//foreach (StatementNode snode2 in ifNode.StatementBlock.Statements)
							//{
								//ifNode.
								StringBuilder sb2 = new StringBuilder();
								ifNode.ToSource(sb2);
								UnityEngine.Debug.Log(sb2);
							//}
						}
						//lastLine = snode.RelatedToken.Line;
						sb = new StringBuilder();
						snode.ToSource(sb);
						string line = sb.ToString();
						UnityEngine.Debug.Log(snode.RelatedToken.Line.ToString() + ": " + line);
						
					}
				}
			}
		}
		
		UnityEngine.Debug.Log("Press any key to continue . . . ");
		Console.ReadKey(true);
	}
}
*/