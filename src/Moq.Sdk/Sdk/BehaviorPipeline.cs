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
	/// Represents the configured behaviors for a mock invocation.
	/// </summary>
	[Serializable]
	public class BehaviorPipeline
	{
		// All this remoting crap is necessary because I'm using the 
		// remoting CallContext so that we're smarter than just 
		// Thread.SetData for multi-threaded runners and code under 
		// test. We need these attributes for test runners that 
		// live inside VS and use cross-AppDomain remoting to run 
		// the tests. Otherwise, the non-serializable implementations 
		// of any of these components will cause the test to fail or 
		// even hang.
		[NonSerialized]
		private IArgumentMatcher[] argumentMatchers;
		[NonSerialized]
		private IBehavior fallbackAtTarget;
		[NonSerialized]
		private IList<IBehavior> beforeTarget = new List<IBehavior>();
		[NonSerialized]
		private IList<IBehavior> atTarget = new List<IBehavior>();
		[NonSerialized]
		private IList<IBehavior> afterTarget = new List<IBehavior>();

		/// <summary>
		/// Initializes a new instance of the <see cref="BehaviorPipeline"/> class.
		/// </summary>
		/// <param name="fallbackAtTarget">The fallback behavior to use when no other behavior is 
		/// active <see cref="AtTarget"/> for a particular invocation.</param>
		/// <param name="argumentMatchers">The argument matchers to use to determine if this 
		/// pipeline should run for a given invocation.</param>
		public BehaviorPipeline(IBehavior fallbackAtTarget, IEnumerable<IArgumentMatcher> argumentMatchers)
		{
			this.fallbackAtTarget = fallbackAtTarget;
			this.argumentMatchers = argumentMatchers.ToArray();
		}

		/// <summary>
		/// Whether this pipeline applies to the given invocation. Performs matching 
		/// of received argument matchers.
		/// </summary>
		public bool AppliesTo(IInvocation invocation)
		{
			if (invocation.Arguments.Length != this.argumentMatchers.Length)
				return false;

			for (int i = 0; i < invocation.Arguments.Length; i++)
			{
				if (!this.argumentMatchers[i].Matches(invocation.Arguments[i]))
					return false;
			}

			return true;
		}

		/// <summary>
		/// The argument matchers to use to determine if this 
		/// pipeline should run for a given invocation.
		/// </summary>		
		public IEnumerable<IArgumentMatcher> ArgumentMatchers { get { return this.argumentMatchers; } }

		/// <summary>
		/// Executes the pipeline for the given invocation.
		/// </summary>
		/// <param name="invocation">The invocation.</param>
		public void ExecuteFor(IInvocation invocation)
		{
			if (!AppliesTo(invocation))
				throw new InvalidOperationException("This behavior pipeline does not apply to the received invocation.");

			ExecuteBehaviors(this.BeforeTarget, invocation);
			
			// We append the fallback behavior at the end of the AtTarget list so far.
			ExecuteBehaviors(this.AtTarget.Concat(new [] { this.FallbackAtTarget }), invocation);

			ExecuteBehaviors(this.AfterTarget, invocation);
		}

		private void ExecuteBehaviors(IEnumerable<IBehavior> behaviors, IInvocation invocation)
		{
			behaviors
				.Where(x => x.IsActiveFor(invocation))
				// Avoid concurrency issues
				.ToArray()
				// Execute lazily one by one, grabbing the resulting action
				.Select(x => x.ExecuteFor(invocation))
				// Only execute as long as we were getting Continue
				.TakeWhile(x => x == BehaviorAction.Continue)
				// This call is the one that actually causes everything to run
				.ToArray();
		}

		/// <summary>
		/// Gets the list of behaviors that will run before the target 
		/// invocation on the mock.
		/// </summary>
		public IList<IBehavior> BeforeTarget { get { return this.beforeTarget; } }

		/// <summary>
		/// Gets the list of behaviors that will run at the target 
		/// invocation on the mock.
		/// </summary>
		public IList<IBehavior> AtTarget { get { return this.atTarget; } }

		/// <summary>
		/// Gets the list of behaviors that will run after the target 
		/// invocation on the mock.
		/// </summary>
		public IList<IBehavior> AfterTarget { get { return this.afterTarget; } }

		// Target must run for non-void methods, so this would 
		// define that default behavior (return default values, 
		// call base, or throw)
		public IBehavior FallbackAtTarget { get { return this.fallbackAtTarget; } }
	}
}
