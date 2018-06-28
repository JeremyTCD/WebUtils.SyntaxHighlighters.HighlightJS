﻿using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.NodeServices.HostingModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JeremyTCD.WebUtils.SyntaxHighlighters.HighlightJS
{
    public class HighlightJSService : IHighlightJSService, IDisposable
    {
        internal const string BUNDLE = "JeremyTCD.WebUtils.SyntaxHighlighters.HighlightJS.bundle.js";
        private readonly INodeServices _nodeServices;

        /// <summary>
        /// Use <see cref="Lazy{T}"/> for thread safe lazy initialization since invoking a JS method through NodeServices
        /// can take several hundred milliseconds. Wrap in a <see cref="Task{T}"/> for asynchrony.
        /// More information on AsyncLazy - https://blogs.msdn.microsoft.com/pfxteam/2011/01/15/asynclazyt/.
        /// </summary>
        private readonly Lazy<Task<HashSet<string>>> _aliases;

        public HighlightJSService(INodeServices nodeServices)
        {
            _nodeServices = nodeServices;
            _aliases = new Lazy<Task<HashSet<string>>>(GetAliasesAsync);
        }

        /// <summary>
        /// Highlights <paramref name="code"/>.
        /// </summary>
        /// <param name="code">Code to highlight.</param>
        /// <param name="languageAlias">A HighlightJS language alias. Visit http://highlightjs.readthedocs.io/en/latest/css-classes-reference.html#language-names-and-aliases 
        /// for the full list of valid language aliases.</param>
        /// <param name="classPrefix">If not null or whitespace, this string will be appended to HighlightJS classes.</param>
        /// <returns>Highlighted <paramref name="code"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="code"/> is null.</exception>
        /// <exception cref="NodeInvocationException">Thrown if a Node error occurs.</exception>
        public virtual async Task<string> HighlightAsync(string code,
            string languageAlias,
            string classPrefix = "hljs-")
        {
            if (code == null)
            {
                throw new ArgumentNullException(nameof(code), Strings.Exception_ParameterCannotBeNull);
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                // Nothing to highlight
                return code;
            }

            if (!await IsValidLanguageAliasAsync(languageAlias).ConfigureAwait(false))
            {
                // languageAlias is invalid
                throw new ArgumentException(string.Format(Strings.Exception_InvalidHighlightJSLanguageAlias, languageAlias));
            }

            try
            {
                return await _nodeServices.InvokeExportAsync<string>(BUNDLE,
                    "highlight",
                    code,
                    languageAlias,
                    string.IsNullOrWhiteSpace(classPrefix) ? "" : classPrefix).ConfigureAwait(false);
            }
            catch (AggregateException exception)
            {
                if (exception.InnerException is NodeInvocationException)
                {
                    throw exception.InnerException;
                }
                throw;
            }
        }

        /// <summary>
        /// Returns true if <paramref name="languageAlias"/> is a valid HighlightJS language alias. Otherwise, returns false.
        /// </summary>
        /// <param name="languageAlias">Language alias to validate. Visit http://highlightjs.readthedocs.io/en/latest/css-classes-reference.html#language-names-and-aliases 
        /// for the full list of valid language aliases.</param>
        /// <returns>true if <paramref name="languageAlias"/> is a valid HighlightJS language alias. Otherwise, false.</returns>
        /// <exception cref="NodeInvocationException">Thrown if a Node error occurs.</exception>
        public virtual async Task<bool> IsValidLanguageAliasAsync(string languageAlias)
        {
            if (string.IsNullOrWhiteSpace(languageAlias))
            {
                return false;
            }

            try
            {
                HashSet<string> aliases = await _aliases.Value.ConfigureAwait(false);

                return aliases.Contains(languageAlias);
            }
            catch (AggregateException exception)
            {
                if (exception.InnerException is NodeInvocationException)
                {
                    throw exception.InnerException;
                }
                throw;
            }
        }

        /// <summary>
        /// Required for lazy initialization.
        /// </summary>
        /// <returns></returns>
        internal virtual async Task<HashSet<string>> GetAliasesAsync()
        {
            string[] aliases = await _nodeServices.InvokeExportAsync<string[]>(BUNDLE, "getAliases").ConfigureAwait(false);

            return new HashSet<string>(aliases);
        }

        public void Dispose()
        {
            _nodeServices.Dispose();
        }
    }
}
