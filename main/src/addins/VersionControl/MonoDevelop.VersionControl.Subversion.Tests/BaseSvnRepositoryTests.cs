//
// RepositoryTests.cs
//
// Author:
//       Therzok <teromario@yahoo.com>
//
// Copyright (c) 2013 Xamarin Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using MonoDevelop.Core;
using MonoDevelop.VersionControl.Tests;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System;

namespace MonoDevelop.VersionControl.Subversion.Tests
{
	[TestFixture]
	public abstract class BaseSvnUtilsTest : BaseRepoUtilsTest
	{
		protected Process svnServe = null;

		[SetUp]
		public override void Setup ()
		{
			Process svnAdmin;
			ProcessStartInfo info;

			// Generate directories and a svn util.
			rootCheckout = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);

			// Create repo in "repo".
			svnAdmin = new Process ();
			info = new ProcessStartInfo ();
			info.FileName = "svnadmin";
			info.Arguments = "create " + rootUrl + Path.DirectorySeparatorChar + "repo";
			info.WindowStyle = ProcessWindowStyle.Hidden;
			svnAdmin.StartInfo = info;
			svnAdmin.Start ();
			svnAdmin.WaitForExit ();

			// Create host (Win32)
			// This needs to be done after doing the svnAdmin creation.
			// And before checkout.
			if (svnServe != null) {
				info = new ProcessStartInfo ();
				info.FileName = "svnserve";
				info.Arguments = "-dr " + rootUrl;
				info.WindowStyle = ProcessWindowStyle.Hidden;
				svnServe.StartInfo = info;
				svnServe.Start ();

				// Create user to auth.
				using (var perm = File. CreateText (rootUrl + Path.DirectorySeparatorChar + "repo" +
				                                    Path.DirectorySeparatorChar + "conf" + Path.DirectorySeparatorChar + "svnserve.conf")) {
					perm.WriteLine ("[general]");
					perm.WriteLine ("anon-access = write");
					perm.WriteLine ("[sasl]");
				}
			}

			// Check out the repository.
			Checkout (rootCheckout, repoLocation);
			repo = GetRepo (rootCheckout, repoLocation);
			DOT_DIR = ".svn";
		}

		[Test]
		public override void RightRepositoryDetection ()
		{
			var path = ((string)rootCheckout).TrimEnd (Path.DirectorySeparatorChar);
			var repo = VersionControlService.GetRepositoryReference (path, null);
 			Assert.That (repo, Is.InstanceOf<SubversionRepository> (), "#1");

			while (!String.IsNullOrEmpty (path)) {
				path = Path.GetDirectoryName (path);
				if (path == null)
					return;
				Assert.IsNull (VersionControlService.GetRepositoryReference (path, null), "#2." + path);
			}

			// Versioned file
			AddFile ("foo", "contents", true, true);
			path = Path.Combine (rootCheckout, "foo");
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#2");

			// Versioned directory
			AddDirectory ("bar", true, true);
			path = Path.Combine (rootCheckout, "bar");
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#3");

			// Unversioned file
			AddFile ("bip", "contents", false, false);
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#4");

			// Unversioned directory
			AddDirectory ("bop", false, false);
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#5");

			// Nonexistent file
			path = Path.Combine (rootCheckout, "do_i_exist");
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#6");

			// Nonexistent directory
			path = Path.Combine (rootCheckout, "do", "i", "exist");
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#6");
		}
	}
}

