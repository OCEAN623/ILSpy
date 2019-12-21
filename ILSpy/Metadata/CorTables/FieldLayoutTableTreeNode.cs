﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.Metadata
{
	internal class FieldLayoutTableTreeNode : ILSpyTreeNode
	{
		private PEFile module;

		public FieldLayoutTableTreeNode(PEFile module)
		{
			this.module = module;
		}

		public override object Text => $"10 FieldLayout ({module.Metadata.GetTableRowCount(TableIndex.FieldLayout)})";

		public override object Icon => Images.Literal;

		public unsafe override bool View(ViewModels.TabPageModel tabPage)
		{
			tabPage.Title = Text.ToString();
			tabPage.SupportsLanguageSwitching = false;

			var view = Helpers.PrepareDataGrid(tabPage);
			var metadata = module.Metadata;

			var list = new List<FieldLayoutEntry>();

			var length = metadata.GetTableRowCount(TableIndex.FieldLayout);
			byte* ptr = metadata.MetadataPointer;
			int metadataOffset = module.Reader.PEHeaders.MetadataStartOffset;
			for (int rid = 1; rid <= length; rid++) {
				list.Add(new FieldLayoutEntry(module, ptr, metadataOffset, rid));
			}

			view.ItemsSource = list;

			tabPage.Content = view;
			return true;
		}

		readonly struct FieldLayout
		{
			public readonly int Offset;
			public readonly FieldDefinitionHandle Field;

			public unsafe FieldLayout(byte* ptr, int fieldDefSize)
			{
				Offset = Helpers.GetValue(ptr, 4);
				Field = MetadataTokens.FieldDefinitionHandle(Helpers.GetValue(ptr + 4, fieldDefSize));
			}
		}

		unsafe struct FieldLayoutEntry
		{
			readonly PEFile module;
			readonly MetadataReader metadata;
			readonly FieldLayout fieldLayout;

			public int RID { get; }

			public int Token => 0x10000000 | RID;

			public int Offset { get; }

			[StringFormat("X8")]
			public int Field => MetadataTokens.GetToken(fieldLayout.Field);

			public string FieldTooltip {
				get {
					ITextOutput output = new PlainTextOutput();
					var context = new Decompiler.Metadata.GenericContext(default(TypeDefinitionHandle), module);
					((EntityHandle)fieldLayout.Field).WriteTo(module, output, context);
					return output.ToString();
				}
			}

			[StringFormat("X8")]
			public int FieldOffset => fieldLayout.Offset;

			public FieldLayoutEntry(PEFile module, byte* ptr, int metadataOffset, int row)
			{
				this.module = module;
				this.metadata = module.Metadata;
				this.RID = row;
				var rowOffset = metadata.GetTableMetadataOffset(TableIndex.FieldLayout)
					+ metadata.GetTableRowSize(TableIndex.FieldLayout) * (row - 1);
				this.Offset = metadataOffset + rowOffset;
				int fieldDefSize = metadata.GetTableRowCount(TableIndex.Field) < ushort.MaxValue ? 2 : 4;
				this.fieldLayout = new FieldLayout(ptr + rowOffset, fieldDefSize);
			}
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.WriteCommentLine(output, "FieldLayouts");
		}
	}
}