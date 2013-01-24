using UnityEngine;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Microsoft.CSharp;
using System.Reflection;
using System.IO;

using System.Linq;
using System.Linq.Expressions;


namespace UIDE.CodeCompletion {
	public class NotDynamicClass {
	    private readonly List<string> values = new List<string>();
	
	    public void AddValue(string value)
	    {
	        values.Add(value);
	    }
	
	    public void ProcessValues()
	    {
	        foreach (var item in values)
	        {
				
	            Debug.Log(item);
	        }
	    }
	}
	
	
	class ExpressionCompiler {
		public void Compile() {
			//float startTime = Time.realtimeSinceStartup;
			
			var provider = CSharpCodeProvider.CreateProvider("c#");
			var options = new CompilerParameters();
			var assemblyContainingNotDynamicClass = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
			Debug.Log(assemblyContainingNotDynamicClass);
			options.ReferencedAssemblies.Add(assemblyContainingNotDynamicClass);
			var results = provider.CompileAssemblyFromSource(options, new[] { 
				@"public class DynamicClass {
					public static void Main() {
						//UnityEngine.Debug.Log(""HELLO"");
					}
				}"
			});
			
			//results.CompiledAssembly.GetType("DynamicClass").GetMethod("Main").Invoke(null,null);
			
			if (results.Errors.Count > 0) {
				foreach (var error in results.Errors) {
					Console.WriteLine(error);
				}
			}
			else {
				
			}
		}
		
	}
}
