// DbManagerTest.OleDb.Access.h

#pragma once

using namespace System;
using namespace System::Data;

using namespace NUnit::Framework;

using namespace Rsdn::Framework::Data;

namespace Cpp
{
	namespace DbManagerTest
	{
		namespace OleDb
		{
			[TestFixture]
			public __gc class Access : public Cpp::DbManagerTest::Test
			{
			public:

				__property String* get_ConfigurationString()
				{
					return "Access.OleDb";
				}
			};
		}
	}
}