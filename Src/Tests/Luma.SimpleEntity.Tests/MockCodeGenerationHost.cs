using System;
using System.Collections.Generic;
using Luma.SimpleEntity;
using Luma.SimpleEntity.Tests.Server.Test.Utilities;
using Luma.SimpleEntity.Tests.Utilities;

namespace Luma.SimpleEntity.Tests
{
    internal class MockCodeGenerationHost : ICodeGenerationHost
    {
        private ILoggingService _loggingService;
        private ISharedCodeService _sharedCodeService;

        public MockCodeGenerationHost() : this(null, null)
        {
        }

        public MockCodeGenerationHost(ILoggingService loggingService, ISharedCodeService sharedCodeService)
        {
            this._loggingService = loggingService ?? new ConsoleLogger();
            this._sharedCodeService = sharedCodeService ?? new MockSharedCodeService(null, null, null);
        }

        internal ILoggingService LoggingService { get { return this._loggingService; } }

        internal ISharedCodeService SharedCodeService { get { return this._sharedCodeService; } }

        #region ILogger members

        public bool HasLoggedErrors
        {
            get { return this.LoggingService.HasLoggedErrors; }
        }

        public void LogError(string message)
        {
            this.LoggingService.LogError(message);
        }

        public void LogException(Exception ex)
        {
            this.LoggingService.LogException(ex);
        }

        public void LogWarning(string message)
        {
            this.LoggingService.LogWarning(message);
        }

        public void LogMessage(string message)
        {
            this.LoggingService.LogMessage(message);
        }

        #endregion // ILogger members

        #region ILoggingService members
        public void LogError(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        {
            this.LoggingService.LogError(message, subcategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber);
        }

        public void LogWarning(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        {
            this.LoggingService.LogWarning(message, subcategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber);
        }


        #endregion // ILoggingService members

        #region ISharedCodeService members

        public CodeMemberShareKind GetTypeShareKind(string typeName)
        {
            return this.SharedCodeService.GetTypeShareKind(typeName);
        }

        public CodeMemberShareKind GetPropertyShareKind(string typeName, string propertyName)
        {
            return this.SharedCodeService.GetPropertyShareKind(typeName, propertyName);
        }

        public CodeMemberShareKind GetMethodShareKind(string typeName, string methodName, IEnumerable<string> parameterTypeNames)
        {
            return this.SharedCodeService.GetMethodShareKind(typeName, methodName, parameterTypeNames);
        }

        #endregion // ISharedCodeService members

    }
}
