using Microsoft.CodeAnalysis;
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
        public void string_interpolation_syntax_error_as_expected()
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
			string selection = ""id, name, title"";
            string where = ""id = '1'"";
            var cmd = new SqlCommand(""SEL \{selection} FROM myTable WHERE \{where}"");
        }
	}
}";
            var expected = new DiagnosticResult
            {
                Id = SqlAnalyzerAnalyzer.DiagnosticId,
                Message = "Incorrect syntax near title.",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                    new DiagnosticResultLocation("Test0.cs", 13, 23)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void stringbuilder_syntax_error_as_expected()
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
			StringBuilder sbl = new StringBuilder();
            sbl.Append(""SEL"");
            sbl.Append("" * "");
            sbl.Append(""FROM user"");
            var cmd2 = new SqlCommand(sbl.ToString());
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
                    new DiagnosticResultLocation("Test0.cs", 15, 24)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
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
        public void Invalid_concatenation_Sql_Reported_In_Constructor_Literal()
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
		string selection = "" * "";
        string where = ""id = '1'"";
        string sql = ""SEL "" + selection + ""WHERE "" + where;

        var cmd2 = new SqlCommand(sql);
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
                    new DiagnosticResultLocation("Test0.cs", 15, 20)
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
		public void Reporting_In_Complex_Assignment_Works()
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
            var cmd = new SqlCommand(""SEL * FROM myTABLE"" + sql);
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
                    new DiagnosticResultLocation("Test0.cs", 12, 23)
                }
            };

			VerifyCSharpDiagnostic(test, expected);
		}


        [TestMethod]
        public void Reporting_In_Complex_Assignment_2_Incorrect_Syntax()
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
			var sql = ""SEL * FROM myTABLE"";
            var cmd = new SqlCommand(sql + "" WHERE X = y"");
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
                    new DiagnosticResultLocation("Test0.cs", 12, 23)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void Reporting_In_Complex_Assignment_2_variables_and_string_Incorrect_Syntax()
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
			var sql = ""SEL * FROM myTABLE"";
            var eq = ""X = y""
            var cmd = new SqlCommand(sql + "" WHERE "" + eq);
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
                    new DiagnosticResultLocation("Test0.cs", 13, 23)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void Reporting_In_Complex_Assignment_2_concatenated_variables_and_string_Syntax_Error()
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
            var eq = ""X = y""
            var sql = ""SEL * FROM myTABLE"" + eq;
            var cmd = new SqlCommand(sql);
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
                    new DiagnosticResultLocation("Test0.cs", 12, 23)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
		public void No_Reporting_In_Valid_Complex_Assignment()
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
            var cmd = new SqlCommand(""SELECT * FROM myTABLE"" + sql);
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