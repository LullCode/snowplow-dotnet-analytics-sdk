﻿/*
 * JsonShredderTest.cs
 * 
 * Copyright (c) 2017 Snowplow Analytics Ltd. All rights reserved.
 * This program is licensed to you under the Apache License Version 2.0,
 * and you may not use this file except in compliance with the Apache License
 * Version 2.0. You may obtain a copy of the Apache License Version 2.0 at
 * http://www.apache.org/licenses/LICENSE-2.0.
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the Apache License Version 2.0 is distributed on
 * an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the Apache License Version 2.0 for the specific
 * language governing permissions and limitations there under.
 * Authors: Devesh Shetty
 * Copyright: Copyright (c) 2017 Snowplow Analytics Ltd
 * License: Apache License Version 2.0
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Snowplow.Analytics.Exceptions;
using Snowplow.Analytics.Json;
using Xunit;

namespace Snowplow.Analytics.Tests.Json
{
    public class JsonShredderTest
    {
        [Fact]
        public void TestParseContexts()
        {
            var actual = "{\n  'schema': 'any',\n  'data': [\n    {\n      'schema': 'iglu:com.acme/duplicated/jsonschema/20-0-5',\n      'data': {\n        'value': 1\n      }\n    },\n    {\n      'schema': 'iglu:com.acme/duplicated/jsonschema/20-0-5',\n      'data': {\n        'value': 2\n      }\n    },\n    {\n      'schema': 'iglu:com.acme/unduplicated/jsonschema/1-0-0',\n      'data': {\n        'type': 'test'\n      }\n    }\n  ]\n}";

            var expected = new Dictionary<string, List<JToken>>()
            {
                {
                    "contexts_com_acme_duplicated_20", new List<JToken>()
                    {
                        JToken.Parse("{'value': 1}"),
                        JToken.Parse("{'value': 2}")
                    }
                },
                {
                    "contexts_com_acme_unduplicated_1", new List<JToken>()
                    {
                        JToken.Parse("{'type': 'test'}")
                    }
                }

            };

            var actualDict = JsonShredder.ParseContexts(actual);
            Assert.Equal(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(actualDict));
        }

        [Fact]
        public void TestContextWithMalformedField()
        {
            SnowplowEventTransformationException exception = null;
            var malformedContext = "{\n  'schema': 'any',\n  'data': [\n    {\n      'schema': 'failing'\n    },\n    {\n      'data': {\n        'value': 2\n      }\n    },\n    {\n      'schema': 'iglu:com.acme/unduplicated/jsonschema/1-0-0'\n    }\n  ]\n}";

            try
            {
                JsonShredder.ParseContexts(malformedContext);
            }
            catch (SnowplowEventTransformationException sete)
            {
                exception = sete;
            }

            Assert.True(exception.ErrorMessages[0].Equals("Could not extract inner data field from custom context."));
        }

        [Fact]
        public void TestContextWithMultipleMalformedFields()
        {
            SnowplowEventTransformationException exception = null;
            var malformedContext = "{\n  'schema': 'any',\n  'data': [\n    {\n      'schema': 'failing',\n      'data': {\n        'value': 1\n      }\n    },\n    {\n      'data': {\n        'value': 2\n      }\n    },\n    {\n      'schema': 'iglu:com.acme/unduplicated/jsonschema/1-0-0'\n    }\n  ]\n}";

            try
            {
                JsonShredder.ParseContexts(malformedContext);
            }
            catch (SnowplowEventTransformationException sete)
            {
                exception = sete;
            }

            Assert.Equal(3, exception.ErrorMessages.Count);
        }

        [Fact]
        public void TestUnstruct()
        {
            var actual = "{\n  'schema': 'any',\n  'data': {\n    'schema': 'iglu:com.snowplowanalytics.snowplow/social_interaction/jsonschema/1-0-0',\n    'data': {\n      'action': 'like',\n      'network': 'fb'\n    }\n  }\n}";

            var expected = JObject.Parse(@"{
                                        'unstruct_event_com_snowplowanalytics_snowplow_social_interaction_1': 
                                            {
											    'action':'like',
											    'network':'fb'
									        }
										}");

            var actualDict = JsonShredder.ParseUnstruct(actual);

            Assert.Equal(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(actualDict));

        }


    }
}
