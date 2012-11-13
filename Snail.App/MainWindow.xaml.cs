using System;
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

using MarkupParser;

namespace MarkupParser.App
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			string testFile = @"..\Snail.Test\PsyStarcraft_channel.htm";
			//string testFile = @"..\Snail.Test\ot.xml";
			string testText = File.ReadAllText(testFile);

			var sw = Stopwatch.StartNew();

			int count = 0;
			for (int i = 0; i < 1; i++)
			{
				count = Document.Parse2(testText);
			}

			sw.Stop();

			var status = string.Format("Parsed {0} tags in {1} ms", count, sw.Elapsed.TotalMilliseconds);
			Trace.WriteLine(status);
			textBlock.Text = status;
		}
	}
}
