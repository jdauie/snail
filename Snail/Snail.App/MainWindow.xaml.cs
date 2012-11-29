﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Snail;
using Snail.Nodes;

namespace Snail.App
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			//ParseTest();
			CompareProjectFilesTest();
		}

		private void ParseTest()
		{
			string testFile;
			testFile = @"..\Snail.Test\PsyStarcraft_channel.htm";
			//testFile = @"..\Snail.Test\kjv-osis\kjv.osis.xml";
			//testFile = @"..\Snail.Test\kjv-usfx\eng-kjv_usfx.xml";
			//testFile = @"..\Snail.Test\ot.xml";
			//testFile = @"..\Snail.Test\TO_core_last_DEM_AR_forests.shp.afe";
			//testFile = @"..\Snail.Test\xhtml1-strict.dtd";
			//testFile = @"..\Snail.Test\Viewer3D.101.vcproj";
			string text = File.ReadAllText(testFile);

			var sw = Stopwatch.StartNew();

			DocumentNode root = null;
			for (int i = 0; i < 1; i++)
			{
				root = Document.Parse(text);
			}

			sw.Stop();

			var status = string.Format("Parsed {0} tags in {1} ms", (root != null) ? 1 : 0, sw.Elapsed.TotalMilliseconds);
			Trace.WriteLine(status);
			textBlock.Text = status;
		}

		private void CompareProjectFilesTest()
		{
			CompareProjectFiles(
				@"..\Snail.Test\Viewer3D.101.vcproj",
				@"..\Snail.Test\Viewer3D.102.vcproj"
				);
		}

		private void CompareProjectFiles(string p1, string p2)
		{
			var text1 = File.ReadAllText(p1);
			var root1 = Document.Parse(text1);
			//File.WriteAllText(@"..\Snail.Test\compare1.txt", root1.ToFormattedString(WhitespaceMode.Insert, "\t"));

			var text2 = File.ReadAllText(p2);
			var root2 = Document.Parse(text2);
			//File.WriteAllText(@"..\Snail.Test\compare2.txt", root2.ToFormattedString(WhitespaceMode.Insert, "\t"));

			// compare trees for differences (not considering order)


			treeView1.ItemsSource = root1.Children;

			var status = string.Format("Compared {0}, {1}", System.IO.Path.GetFileName(p1), System.IO.Path.GetFileName(p2));
			Trace.WriteLine(status);
			textBlock.Text = status;
		}
	}
}
