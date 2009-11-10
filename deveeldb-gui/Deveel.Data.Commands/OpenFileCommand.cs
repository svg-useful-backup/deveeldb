﻿using System;
using System.Windows.Forms;

using Deveel.Data.Commands;

using WeifenLuo.WinFormsUI.Docking;

namespace Deveel.Data {
	[CommandSmallImage("Deveel.Data.Images.folder_page.png")]
	public sealed class OpenFileCommand : Command {
		public OpenFileCommand() 
			: base("&Open File", Keys.Control | Keys.O, "Ctrl+O") {
		}

		public override void Execute() {
			string defaultFileFilter = (string) Settings.GetProperty(SettingsProperties.DefaultFileFilter);

			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
			openFileDialog.Filter = defaultFileFilter;
			openFileDialog.CheckFileExists = true;
			if (openFileDialog.ShowDialog(HostWindow.Form) == DialogResult.OK) {
				IFileEditorResolver resolver = (IFileEditorResolver)Services.Resolve(typeof(IFileEditorResolver));
				IEditor editor = resolver.ResolveEditor(openFileDialog.FileName);
				editor.FileName = openFileDialog.FileName;
				editor.LoadFile();
				HostWindow.DisplayDockedForm(editor as DockContent);
			}
		}
	}
}