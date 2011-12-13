﻿/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2011 MindTouch, Inc.
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
using MindTouch.Tasking;

namespace MindTouch.Dream.Services.PubSub {
    [Obsolete("The PubSub subsystem has been deprecated and will be removed in v3.0")]
    public class ImmediatePubSubDispatchQueue : IPubSubDispatchQueue {

        //--- Fields ---
        private readonly Func<DispatchItem, Result<bool>> _dequeueHandler;

        //--- Constructors ---
        public ImmediatePubSubDispatchQueue(Func<DispatchItem, Result<bool>> dequeueHandler) {
            if(dequeueHandler == null) {
                throw new ArgumentNullException("dequeueHandler");
            }
            _dequeueHandler = dequeueHandler;
        }

        //--- Properties ---
        public TimeSpan FailureWindow {
            get { return TimeSpan.Zero; }
        }

        //--- Methods ---
        public void Enqueue(DispatchItem item) {
            _dequeueHandler(item);
        }

        public void Dispose() { }
    }
}