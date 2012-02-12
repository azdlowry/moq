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
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators;
using System.Diagnostics.CodeAnalysis;

using CastleInterceptor = Castle.DynamicProxy.IInterceptor;
using CastleInvocation = Castle.DynamicProxy.IInvocation;
using Moq.Sdk;

namespace Moq
{
	/// <summary>
	/// Provides a proxy factory for mocks based on Castle Dynamic Proxy.
	/// </summary>
	internal class CastleProxyFactory : IProxyFactory
	{
		private static readonly ProxyGenerator generator = CreateProxyGenerator();

		[SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "By Design")]
		static CastleProxyFactory()
		{
#pragma warning disable 618
			AttributesToAvoidReplicating.Add<SecurityPermissionAttribute>();
#pragma warning restore 618

#if !SILVERLIGHT
			AttributesToAvoidReplicating.Add<ReflectionPermissionAttribute>();
			AttributesToAvoidReplicating.Add<PermissionSetAttribute>();
			AttributesToAvoidReplicating.Add<System.Runtime.InteropServices.MarshalAsAttribute>();
#if !NET3x
			AttributesToAvoidReplicating.Add<System.Runtime.InteropServices.TypeIdentifierAttribute>();
#endif
#endif
		}

		public T CreateProxy<T>(IProxied interceptable, Type[] interfaces, object[] arguments)
		{
			var mockType = typeof(T);
			interfaces = interfaces.Concat(new[] { typeof(IMocked) }).ToArray();

			if (mockType.IsInterface)
			{
				return (T)generator.CreateInterfaceProxyWithoutTarget(mockType, interfaces, new ForwardingInterceptor(interceptable));
			}

			try
			{
				return (T)generator.CreateClassProxy(mockType, interfaces, new ProxyGenerationOptions(), arguments, new ForwardingInterceptor(interceptable));
			}
			catch (TypeLoadException e)
			{
				throw;
				//throw new ArgumentException(Resources.InvalidMockClass, e);
			}
			catch (MissingMethodException e)
			{
				throw;
				//throw new ArgumentException(Resources.ConstructorNotFound, e);
			}
		}

		private static ProxyGenerator CreateProxyGenerator()
		{
			return new ProxyGenerator();
		}

		// Forwards intercepted calls to the proxied mock.
		private class ForwardingInterceptor : CastleInterceptor
		{
			private IProxied mock;

			internal ForwardingInterceptor(IProxied mock)
			{
				this.mock = mock;
			}

			public void Intercept(CastleInvocation invocation)
			{
				if (invocation.Method.DeclaringType == typeof(IMocked))
				{
					// "Mixin" of IMocked.Mock
					invocation.ReturnValue = this.mock;
					return;
				}

				this.mock.Execute(new InvocationAdapter(invocation, this.mock));
			}
		}

		// Adapts the Castle invocation contract to Moq's.
		[Serializable]
		private class InvocationAdapter : Moq.Sdk.IInvocation
		{
			[NonSerialized]
			private CastleInvocation invocation;
			[NonSerialized]
			private IProxied mock;

			internal InvocationAdapter(CastleInvocation invocation, IProxied mock)
			{
				this.invocation = invocation;
				this.mock = mock;
			}

			public IMock Mock
			{
				get { return this.mock as IMock; }
			}

			public object[] Arguments
			{
				get { return this.invocation.Arguments; }
			}

			public MethodInfo Method
			{
				get { return this.invocation.Method; }
			}

			public object ReturnValue
			{
				get { return this.invocation.ReturnValue; }
				set { this.invocation.ReturnValue = value; }
			}

			public void InvokeBase()
			{
				this.invocation.Proceed();
			}

			public object Target
			{
				get { return this.invocation.InvocationTarget; }
			}
		}
	}
}