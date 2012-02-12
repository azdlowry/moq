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
using Moq.Sdk;
using System.Diagnostics;

namespace Moq
{
	/// <summary>
	/// Allows creating mocks.
	/// </summary>
	public abstract class Mock : BaseMock
	{
		/// <summary>
		/// Creates a mock of the given type <typeparamref name="T"/>.
		/// </summary>
		/// <returns>An instance of the mocked object.</returns>
		public static T Of<T>()
			where T : class
		{
			return new Mock<T>(new CastleProxyFactory()).Object;
		}
	}

	public class Mock<T> : Mock
		where T : class
	{
		private IProxyFactory proxyFactory;
		private T instance;

		internal Mock(IProxyFactory proxyFactory)
		{
			this.proxyFactory = proxyFactory;
		}

		public T Object
		{
			// Lazily creates the proxy on first acccess. 
			// This is needed because we need to allow the 
			// user to add more interfaces to the mock before 
			// creating the proxy.
			get { return this.instance ?? this.InitializeInstance(); }
		}

		protected override Type MockedType { get { return typeof(T); } }

		private T InitializeInstance()
		{
			// This is required to prevent Moq from 
			// generating too much garbage for customers 
			// using Pex.
			return PexProtector.Invoke(() =>
			{
				this.instance = proxyFactory.CreateProxy<T>(
					this,
					new Type[0],
					new object[0]
					// TODO: add support for adding interfaces.
					/*this.ImplementedInterfaces.ToArray(),
					this.constructorArguments */);

				return this.instance;
			});
		}
	}
}
