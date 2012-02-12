#region BSD License
/*
Copyright (c) 2007. Clarius Consulting, Manas Technology Solutions, InSTEDD
http://moq.me
All rights reserved.

Redistribution and use in source and binary forms, 
with or without modification, are permitted provided 
that the following conditions are met:

    * Redistributions of source code must retain the 
    above copyright notice, this list of conditions and 
    the following disclaimer.

    * Redistributions in binary form must reproduce 
    the above copyright notice, this list of conditions 
    and the following disclaimer in the documentation 
    and/or other materials provided with the distribution.

    * Neither the name of Clarius Consulting, Manas Technology 
	Solutions or InSTEDD nor the names of its contributors 
	may be used to endorse or promote products derived from 
	this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND 
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR 
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF 
SUCH DAMAGE.

See also: http://www.opensource.org/licenses/bsd-license.php
*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moq.Sdk
{
	/// <summary>
	///	Used by static extension methods to provide fluent APIs  
	///	without using setup expressions.
	/// </summary>
	public static class CallContext
	{
		private static ContextData<IInvocation> lastInvocation = ContextData.Create<IInvocation>();
		private static ContextData<Stack<IArgumentMatcher>> argumentMatchers = ContextData.Create(new Stack<IArgumentMatcher>());
		private static ContextData<BehaviorPipeline> lastPipeline = ContextData.Create<BehaviorPipeline>();

		/// <summary>
		/// Gets or sets the last invocation performed on the mock.
		/// </summary>
		public static IInvocation LastInvocation
		{
			get { return lastInvocation.Value; }
			internal set { lastInvocation.Value = value; lastPipeline.Value = null; }
		}

		/// <summary>
		/// Adds an argument matcher for the current mock invocation.
		/// </summary>
		public static void AddMatcher(IArgumentMatcher matcher)
		{
			argumentMatchers.Value.Push(matcher);
			lastPipeline.Value = null;
		}

		/// <summary>
		/// Gets the behavior pipeline for the current invocation.
		/// </summary>
		/// <returns></returns>
		public static BehaviorPipeline GetBehavior()
		{
			if (lastPipeline.Value != null)
				return lastPipeline.Value;

			// Use last invocation, argument matchers, 
			// actual invocation arguments, build 
			// list of matchers for all arguments, 
			// and add pipeline to mock.
			// Clean last invocation and matchers, 
			// as well as the actual mock invocation 
			// that was tracked, as it was used 
			// for recording purposes.
			if (lastInvocation.Value == null)
				throw new InvalidOperationException("There is no mock being called.");

			var currentMatchers = argumentMatchers.Value;
			var finalMatchers = new List<IArgumentMatcher>();
			var invocation = lastInvocation.Value;
			var parameters = invocation.Method.GetParameters();

			for (int i = 0; i < invocation.Arguments.Length; i++)
			{
				var argument = invocation.Arguments[i];
				var parameter = parameters[i];

				if (Object.Equals(argument, DefaultValue.For(parameter.ParameterType)) &&
					currentMatchers.Count != 0 &&
					parameter.ParameterType.IsAssignableFrom(currentMatchers.Peek().ArgumentType))
				{
					finalMatchers.Add(currentMatchers.Pop());
				}
				else
				{
					finalMatchers.Add(new Arguments.ConstantMatcher(parameter.ParameterType, argument));
				}
			}

			lastPipeline.Value = new BehaviorPipeline(new Behaviors.ReturnDefaultValue(), finalMatchers);

			return lastPipeline.Value;
		}
	}
}
