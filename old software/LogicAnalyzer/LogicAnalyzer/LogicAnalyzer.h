// LogicAnalyzer.h

#pragma once

using namespace System;

namespace LogicAnalyzer {

	public class Class1
	{
	public:
		static __declspec(dllexport) void Initialize();

		static __declspec(dllexport) bool stop();

		static __declspec(dllexport) bool startRead();

		static __declspec(dllexport) bool startWrite();

		static __declspec(dllexport) int readByte();
	};
}
