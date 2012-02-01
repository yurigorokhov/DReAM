﻿/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2012 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Reflection;

using MindTouch.Dream;
using MindTouch.Data;

namespace MindTouch.Dream.Test {

    internal class TestDataUpdater : ADataUpdater {

        //--- Constructors ---
        public TestDataUpdater(string version) {
            if(string.IsNullOrEmpty(version)) {
                _targetVersion = null;
            } else {
                _targetVersion = new VersionInfo(version);
                if(!_targetVersion.IsValid) {
                    throw new VersionInfoException(_targetVersion);
                }
            }
        }

        //--- Methods ---
        public override void TestConnection() { }
    }

    [DataUpgrade]
    internal class DummyUpgradeClass {

        public void CustomMethod1() { }

        [EffectiveVersion("10.0.0")]
        public void UpgradeMethod1() { }

        [EffectiveVersion("10.0.1")]
        public void UpgradeMethod2() { }

        [EffectiveVersion("10.0.0")]
        public void UpgradeMethod3() { }

        [EffectiveVersion("9.0.0")]
        public void UpgradeMethod4() { }

        [EffectiveVersion("11.0.0")]
        public void UpgradeMethod5() { }

        [EffectiveVersion("11.3.0")]
        public void UpgradeMethod6() { }

        [EffectiveVersion("9.8.0")]
        public void UpgradeMethod7() { }
    }

    [TestFixture]
    public class DataUpdaterTests {

        //--- Fields ---

        private Assembly _testAssembly;
        private TestDataUpdater _dataUpdater;

        //--- Methods ---

        [TestFixtureSetUp]
        public void Init() {
            _testAssembly = Assembly.GetExecutingAssembly();
            _dataUpdater = new TestDataUpdater("100.0.0");
            _dataUpdater.LoadMethods(_testAssembly);
        }

        [Test]
        public void load_methods_using_reflection() {
            var methods = _dataUpdater.GetMethods();
            Assert.IsTrue(methods.Count > 0, "No methods were loaded");
        }

        [Test]
        public void loaded_methods_proper_order() {
            
            // Load versions of methods into an array
            var methods = _dataUpdater.GetMethods();
            var versionArray = new VersionInfo[methods.Count];
            for(int i = 0; i < methods.Count; i++) {
                var methodInfo = _dataUpdater.GetMethodInfo(methods[i]);
                var attributes = methodInfo.GetMethodInfo.GetCustomAttributes(false);
                Assert.IsTrue(attributes.Count() > 0, String.Format("Method {0} does not have any attributes", methodInfo.GetMethodInfo.Name));
                Assert.IsTrue(attributes.First() is EffectiveVersionAttribute, String.Format("Method {0} does not have the proper attribute", methodInfo.GetMethodInfo.Name));
                versionArray[i] = new VersionInfo(((EffectiveVersionAttribute)attributes.First()).VersionString);
            }

            // Check that version array is in sorted order
            for(int i = 0; i < versionArray.Count() - 1; i++) {
                Assert.IsTrue(versionArray[i].CompareTo(versionArray[i+1]).Change == VersionChange.None || versionArray[i].CompareTo(versionArray[i + 1]).Change == VersionChange.Downgrade, "Methods are being returned in the wrong order");
            }
        }
    }
}
