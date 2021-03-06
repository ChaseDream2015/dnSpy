/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Text;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Debugger.DotNet.Disassembly {
	sealed class DecompilerOutputImpl : IDecompilerOutput {
		readonly StringBuilder sb;
		int indentLevel;
		bool addIndent;
		uint methodToken;
		MethodDebugInfo methodDebugInfo;
		MethodDebugInfo kickoffMethodDebugInfo;

		const string TAB_SPACES = "    ";

		public int Length => sb.Length;
		public int NextPosition => sb.Length + (addIndent ? indentLevel * TAB_SPACES.Length : 0);
		public bool UsesCustomData => true;

		public DecompilerOutputImpl() {
			sb = new StringBuilder();
			addIndent = true;
		}

		internal void Clear() {
			sb.Length = 0;
			indentLevel = 0;
			addIndent = true;
			methodToken = 0;
			methodDebugInfo = null;
			kickoffMethodDebugInfo = null;
		}

		public void Initialize(uint methodToken) => this.methodToken = methodToken;
		public (MethodDebugInfo debugInfo, MethodDebugInfo stateMachineDebugInfoOrNull) TryGetMethodDebugInfo() {
			if (methodDebugInfo != null) {
				if (kickoffMethodDebugInfo != null)
					return (kickoffMethodDebugInfo, methodDebugInfo);
				return (methodDebugInfo, null);
			}
			return default;
		}

		public void AddCustomData<TData>(string id, TData data) {
			if (id == PredefinedCustomDataIds.DebugInfo && data is MethodDebugInfo debugInfo) {
				if (debugInfo.Method.MDToken.Raw == methodToken)
					methodDebugInfo = debugInfo;
				else if (debugInfo.KickoffMethod?.MDToken.Raw == methodToken) {
					var m = debugInfo.KickoffMethod;
					var body = m.Body;
					int bodySize = body?.GetCodeSize() ?? 0;
					var scope = new MethodDebugScope(new ILSpan(0, (uint)bodySize), Array.Empty<MethodDebugScope>(), Array.Empty<SourceLocal>(), Array.Empty<ImportInfo>(), Array.Empty<MethodDebugConstant>());
					kickoffMethodDebugInfo = new MethodDebugInfo(debugInfo.CompilerName, debugInfo.DecompilerSettingsVersion, StateMachineKind.None, m, null, null, Array.Empty<SourceStatement>(), scope, null, null);
					methodDebugInfo = debugInfo;
				}
			}
		}

		public void DecreaseIndent() => indentLevel--;
		public void IncreaseIndent() => indentLevel++;

		public void WriteLine() {
			addIndent = true;
			sb.Append(Environment.NewLine);
		}

		void AddIndent() {
			if (!addIndent)
				return;
			addIndent = false;
			for (int i = 0; i < indentLevel; i++)
				sb.Append(TAB_SPACES);
		}

		void AddText(string text, object color) {
			if (addIndent)
				AddIndent();
			sb.Append(text);
		}

		void AddText(string text, int index, int length, object color) {
			if (addIndent)
				AddIndent();
			sb.Append(text, index, length);
		}

		public void Write(string text, object color) => AddText(text, color);
		public void Write(string text, int index, int length, object color) => AddText(text, index, length, color);

		public void Write(string text, object reference, DecompilerReferenceFlags flags, object color) =>
			Write(text, 0, text.Length, reference, flags, color);

		public void Write(string text, int index, int length, object reference, DecompilerReferenceFlags flags, object color) {
			if (addIndent)
				AddIndent();
			AddText(text, index, length, color);
		}

		public override string ToString() => sb.ToString();
	}
}
