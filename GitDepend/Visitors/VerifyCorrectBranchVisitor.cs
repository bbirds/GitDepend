﻿using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using GitDepend.Busi;
using GitDepend.Configuration;
using GitDepend.Resources;

namespace GitDepend.Visitors
{
    /// <summary>
    /// An implementation of <see cref="IVisitor"/> that verifies that the correct branch
    /// has been been checked out on all dependencies.
    /// </summary>
    public class VerifyCorrectBranchVisitor : NamedDependenciesVisitor
    {
        private readonly IGit _git;
        private readonly IFileSystem _fileSystem;
        private readonly IConsole _console;

        /// <summary>
        /// Creates a new <see cref="VerifyCorrectBranchVisitor"/>
        /// </summary>
        /// <param name="whitelist"></param>
        public VerifyCorrectBranchVisitor(IList<string> whitelist) : base(whitelist)
        {
            _git = DependencyInjection.Resolve<IGit>();
            _fileSystem = DependencyInjection.Resolve<IFileSystem>();
            _console = DependencyInjection.Resolve<IConsole>();
        }

        #region Overrides of NamedDependenciesVisitor

        /// <summary>
        /// Provides the custom hook for VisitDependency. This will only be called if the dependency
        /// was specified in the whitelist.
        /// </summary>
        /// <param name="directory">The directory of the project.</param>
        /// <param name="dependency">The <see cref="Dependency"/> to visit.</param>
        /// <returns></returns>
        protected override ReturnCode OnVisitDependency(string directory, Dependency dependency)
        {
            _git.WorkingDirectory = _fileSystem.Path.Combine(directory, dependency.Directory);
            var currBranch = _git.GetCurrentBranch();

            var code = ReturnCode.Success;

            _console.WriteLine(strings.VERIFYING_CHECKED_OUT_BRANCH);

            if (currBranch != dependency.Branch)
            {
                var oldColor = _console.ForegroundColor;
                _console.ForegroundColor = ConsoleColor.Red;
                _console.WriteLine(strings.INVALID_BRANCH);
                _console.WriteLine(strings.EXPECTED_BRANCH_BUT_WAS_BRANCH, dependency.Branch, currBranch);
                _console.ForegroundColor = oldColor;

                bool goodChoice = false;
                do
                {
                    _console.WriteLine(strings.WHAT_SHOULD_I_DO);
                    _console.WriteLine(strings.UPDATE_CONFIG + currBranch);
                    _console.WriteLine(strings.SWITCH_BRANCH_OPTION + dependency.Branch);
                    _console.WriteLine(strings.GIVE_UP_OPTION);
                    _console.Write("> ");
                    var choice = _console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            goodChoice = true;
                            code = dependency.SyncConfigWithCurrentBranch(directory);
                            break;
                        case "2":
                            goodChoice = true;
                            code = SwitchBranch(directory, dependency);
                            break;
                        case "3":
                            goodChoice = true;
                            code = ReturnCode.InvalidBranchCheckedOut;
                            break;
                        default:
                            _console.WriteLine(strings.TRY_AGAIN);
                            break;
                    }
                } while (!goodChoice);
            }
            else
            {
                _console.WriteLine(strings.LOOKS_GOOD);
            }
            _console.WriteLine();

            return code;
        }

        private ReturnCode SwitchBranch(string directory, Dependency dependency)
        {
            _git.WorkingDirectory = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(directory, dependency.Directory));
            return _git.Checkout(dependency.Branch, false);
        }

        #endregion
    }
}