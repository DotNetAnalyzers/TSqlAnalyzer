using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using TSqlAnalyzer;

namespace TSqlAnalyzer.Test
{
	[TestClass]
	public class UnitTest : CodeFixVerifier
	{

		//No diagnostics expected to show up
		[TestMethod]
		public void No_Diagnostics_Expected()
		{
			var test = @"";

			VerifyCSharpDiagnostic(test);
		}

		//Diagnostics checked for
		[TestMethod]
		public void Valid_Sql_Works()
		{
			var test = @"
using System;
using System.Data.SqlClient;    

namespace ConsoleApplication1
{
	class TypeName
	{
		private void AnalyzerTest()
		{
			var cmd = new SqlCommand(""SELECT * FROM MyTable;"");   
		}
	}
}";
			VerifyCSharpDiagnostic(test);
		}

        //Diagnostics checked for
        [TestMethod]
        public void Concat_Strings_Works()
        {
            var test = @"
using System;
using System.Data.SqlClient;    

namespace ConsoleApplication1
{
	class TypeName
	{
		private void AnalyzerTest()
		{
			var cmd = new SqlCommand(""SELECT id FROM userTable"" + "" where id = '1'"");   
		}
	}
}";
            VerifyCSharpDiagnostic(test);
        }


        [TestMethod]
		public void Invalid_Sql_Reported_In_Constructor_Literal()
		{
			var test = @"
using System;
using System.Data.SqlClient;

namespace ConsoleApplication1
{
class TypeName
{
	private void AnalyzerTest()
	{
		var cmd = new SqlCommand(""SEL * FROM MyTable;"");   
	}
}
}";
			var expected = new DiagnosticResult
			{
				Id = SqlAnalyzerAnalyzer.DiagnosticId,
				Message = "Incorrect syntax near SEL.",
				Severity = DiagnosticSeverity.Error,
				Locations =
					new[] {
					new DiagnosticResultLocation("Test0.cs", 11, 28)
				}
			};

			VerifyCSharpDiagnostic(test, expected);
		}


		[TestMethod]
		public void Invalid_Sql_Reported_In_Simple_Assignment()
		{
			var test = @"
using System;
using System.Data.SqlClient;

namespace ConsoleApplication1
{
class TypeName
{
	private void AnalyzerTest()
	{
		var cmd = new SqlCommand();
        cmd.CommandText = ""SEL * FROM MyTable;"";
	}
}
}";
			var expected = new DiagnosticResult
			{
				Id = SqlAnalyzerAnalyzer.DiagnosticId,
				Message = "Incorrect syntax near SEL.",
				Severity = DiagnosticSeverity.Error,
				Locations =
					new[] {
					new DiagnosticResultLocation("Test0.cs", 12, 27)
				}
			};

			VerifyCSharpDiagnostic(test, expected);
		}


		[TestMethod]
		public void No_Reporting_In_Complex_Assignment_Currently()
		{
			var test = @"
using System;
using System.Data.SqlClient;

namespace ConsoleApplication1
{
class TypeName
{
	private void AnalyzerTest()
	{
			var sql = "" WHERE X = y"";
            var cmd = new SqlCommand(""SEL * FROM TABLE"" + sql);
		}
	}
}";
			VerifyCSharpDiagnostic(test);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new SqlAnalyzerAnalyzer();
		}
	}
}