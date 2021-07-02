﻿using System;
using System.Collections.Generic;
using System.Text;

namespace AzStorage.Core.Texting
{
    public static class ErrorTextProvider
    {
        public const string Unexpected_value = "Unexpected value";
        public const string Error_retrieving_entities = "Error retrieving entities";
        public const string Version_of_collection_modified = "Version of collection has been modified";
        public const string Operation_not_allowed = "Operation not allowed";
        public const string Current_keys_same_as_new_keys = "Current keys are the same as the new keys";
        public const string Can_not_load_create_table = "Can't load or create table";
    }
}
