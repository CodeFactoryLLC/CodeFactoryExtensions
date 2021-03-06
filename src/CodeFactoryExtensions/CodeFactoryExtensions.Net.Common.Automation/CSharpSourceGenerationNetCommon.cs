﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeFactory;
using CodeFactory.DotNet.CSharp;
using CodeFactory.DotNet.CSharp.FormattedSyntax;
using CodeFactoryExtensions.Formatting.CSharp;
namespace CodeFactoryExtensions.Net.Common.Automation
{
    /// <summary>
    /// Helper class that provides generation of C# source code from csharp model objects.
    /// </summary>
    public static class CSharpSourceGenerationNetCommon
    {
        /// <summary>
        /// Generates the source code for a method that supports a standard type catch block, and bounds checking as well as the ability to support Microsoft extensions logging.
        /// </summary>
        /// <param name="memberData">Method data to be generated.</param>
        /// <param name="manager">The namespace manager to use for namespace management with type declarations.</param>
        /// <param name="includeBoundsCheck">Flag that determines if string and nullable type bounds checking should be included in a method implementation.</param>
        /// <param name="supportAsyncKeyword">Flag that determines if methods should be implemented with the async keyword when supported by the method implementation.</param>
        /// <param name="security">The security level to add to the generated source code. Will default to public if not provided</param>
        /// <param name="includeLogging">Determines if logging will be added to the method. This will be set to a default of true.</param>
        /// <param name="loggerName">Sets the name of the logger to be used in the method. Will default to _logger if not provided.</param>
        /// <param name="includeKeywords">Determines if the models current keywords should be added to the source code, default if false.</param>
        /// <param name="includeAbstraction">If keywords are to be included, determines if the abstract keyword should be used or ignored, default is false.</param>
        /// <param name="requireStaticKeyword">Adds the static keyword to the signature, default is false.</param>
        /// <param name="requireSealedKeyword">Adds the sealed keyword to the signature, default is false.</param>
        /// <param name="requireAbstractKeyword">Adds the abstract keyword to the signature, default is false.</param>
        /// <param name="requireOverrideKeyword">Adds the override keyword to the signature, default is false.</param>
        /// <param name="requireVirtualKeyword">Adds the virtual keyword to the signature, default is false.</param>
        /// <returns>The fully formatted method source code or null if the member could not be implemented.</returns>
        public static string GenerateStandardMethodSourceCode(CsMethod memberData, NamespaceManager manager, bool includeBoundsCheck, bool supportAsyncKeyword ,
            CsSecurity security = CsSecurity.Public, bool includeLogging = true, string loggerName = null, bool includeKeywords = false,
            bool includeAbstraction = false, bool requireStaticKeyword = false, bool requireSealedKeyword = false, bool requireAbstractKeyword = false,
            bool requireOverrideKeyword = false, bool requireVirtualKeyword = false)
        {
            //Bounds checking to make sure all data that is needed is provided. If any required data is missing will return null.
            if (memberData == null) return null;
            if (!memberData.IsLoaded) return null;
            if (manager == null) return null;

            string logger = null;

            if (includeLogging)
                logger = string.IsNullOrEmpty(loggerName) ? NetConstants.DefaultClassLoggerName : loggerName;

            //C# helper used to format output syntax. 
            var formatter = new CodeFactory.SourceFormatter();

            //Using the formatter helper to generate a method signature.
            string methodSyntax = supportAsyncKeyword
                ? memberData.CSharpFormatMethodSignature(manager, true, true, security,includeKeywords,
                    includeAbstraction,requireStaticKeyword,requireSealedKeyword,requireAbstractKeyword,
                    requireOverrideKeyword,requireVirtualKeyword)
                : memberData.CSharpFormatMethodSignature(manager, false, true, security,includeKeywords, 
                    includeAbstraction, requireStaticKeyword, requireSealedKeyword,
                    requireAbstractKeyword, requireOverrideKeyword, requireVirtualKeyword);

            //If the method syntax was not created return.
            if (string.IsNullOrEmpty(methodSyntax)) return null;

            //If the member has document then will build the documentation.
            if (memberData.HasDocumentation)
                //Using a documentation helper that will generate an enumerator that will output all XML documentation for the member.
                foreach (var documentation in memberData.CSharpFormatXmlDocumentationEnumerator())
                {
                    //Appending each xml document line to the being of the member definition.
                    formatter.AppendCodeLine(0, documentation);
                }

            //The member has attributes assigned to it, append the attributes.
            if (memberData.HasAttributes)
                //Using a documentation helper that will generate an enumerator that will output each attribute definition.
                foreach (var attributeSyntax in memberData.Attributes.CSharpFormatAttributeDeclarationEnumerator(manager))
                {
                    //Appending each attribute definition before the member definition.
                    formatter.AppendCodeLine(0, attributeSyntax);
                }

            //Adding the method declaration
            formatter.AppendCodeLine(0, methodSyntax);
            formatter.AppendCodeLine(0, "{");

            //Adding enter logging if logging is enabled.
            if (includeLogging)
            {
                formatter.AppendCodeLine(1, $"{logger}.LogInformation(\"Entering\");");
                formatter.AppendCodeLine(0);
            }

            //Processing each parameter for bounds checking if bounds checking is enabled.
            if (includeBoundsCheck & memberData.HasParameters)
            {

                foreach (ICsParameter paramData in memberData.Parameters)
                {
                    //If the parameter has a default value then continue will not bounds check parameters with a default value.
                    if (paramData.HasDefaultValue) continue;

                    //If the parameter is a string type add the following bounds check
                    if (paramData.ParameterType.WellKnownType == CsKnownLanguageType.String)
                    {
                        //Adding an if check 
                        formatter.AppendCodeLine(1, $"if(string.IsNullOrEmpty({paramData.Name}))");
                        formatter.AppendCodeLine(1, "{");

                        //If logging was included add error logging and exit logging
                        if (includeLogging)
                        {
                            formatter.AppendCodeLine(2,
                                $"{logger}.LogError($\"The parameter {{nameof({paramData.Name})}} was not provided. Will raise an argument exception\");");
                            formatter.AppendCodeLine(2, $"{logger}.LogInformation(\"Exiting\");");
                        }
                        //Adding a throw of an argument null exception
                        formatter.AppendCodeLine(2, $"throw new ArgumentNullException(nameof({paramData.Name}));");
                        formatter.AppendCodeLine(1, "}");
                        formatter.AppendCodeLine(0);
                    }

                    // Check to is if the parameter is not a value type or a well know type if not then go ahead and perform a null bounds check.
                    if (!paramData.ParameterType.IsValueType & !paramData.ParameterType.IsWellKnownType)
                    {
                        //Adding an if check 
                        formatter.AppendCodeLine(1, $"if({paramData.Name} == null)");
                        formatter.AppendCodeLine(1, "{");

                        //If logging was included add error logging and exit logging
                        if (includeLogging)
                        {
                            formatter.AppendCodeLine(2,
                                $"{logger}.LogError($\"The parameter {{nameof({paramData.Name})}} was not provided. Will raise an argument exception\");");
                            formatter.AppendCodeLine(2, $"{logger}.LogInformation(\"Exiting\");");
                        }

                        //Adding a throw of an argument null exception
                        formatter.AppendCodeLine(2, $"throw new ArgumentNullException(nameof({paramData.Name}));");
                        formatter.AppendCodeLine(1, "}");
                        formatter.AppendCodeLine(0);
                    }
                }
            }

            //Formatting standard try block for method
            formatter.AppendCodeLine(1, "try");
            formatter.AppendCodeLine(1, "{");
            formatter.AppendCodeLine(2, "//TODO: add execution logic here");
            formatter.AppendCodeLine(1, "}");

            //Formatting standard catch block for method
            formatter.AppendCodeLine(1, "catch (Exception unhandledException)");
            formatter.AppendCodeLine(1, "{");

            //If logging is supported capture the exception and log it as an error. Notify that an error has occured and log it . then log exiting the method and throw a scrubbed exception.
            if (includeLogging)
            {
                formatter.AppendCodeLine(2, $"{logger}.LogError(unhandledException, \"An unhandled exception occured, see the exception for details.Will throw a UnhandledLogicException\");");
                formatter.AppendCodeLine(2, $"{logger}.LogInformation(\"Exiting\");");
                formatter.AppendCodeLine(2, "throw new Exception(\"An unhandled error occured, check the logs for details.\");");
            }
            else
            {
                formatter.AppendCodeLine(2, "//TODO: Add exception handling for unhandledException");
            }
            
            
            formatter.AppendCodeLine(1, "}");
            formatter.AppendCodeLine(0);

            //If logging add a logging exit statement.
            if (includeLogging) formatter.AppendCodeLine(1, $"{logger}.LogInformation(\"Exiting\");");

            //Add an exception for not implemented until the developer updates the logic.
            formatter.AppendCodeLine(1, "throw new NotImplementedException();");
            formatter.AppendCodeLine(0);

            //if the return type is not void then add a to do message for the developer to add return logic.
            if (!memberData.IsVoid)
            {
                formatter.AppendCodeLine(1, "//TODO: add return logic here");
            }
            formatter.AppendCodeLine(0, "}");
            formatter.AppendCodeLine(0);

            //Returning the fully formatted method.
            return formatter.ReturnSource();
        }

    }
}
