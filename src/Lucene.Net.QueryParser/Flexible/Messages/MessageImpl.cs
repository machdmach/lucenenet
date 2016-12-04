﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Messages
{
    /// <summary>
    /// Default implementation of Message interface.
    /// For Native Language Support (NLS), system of software internationalization.
    /// </summary>
    public class MessageImpl : IMessage
    {
        private string key;

        private object[] arguments = new object[0];

        public MessageImpl(string key)
        {
            this.key = key;

        }

        public MessageImpl(string key, params object[] args)
            : this(key)
        {
            this.arguments = args;
        }


        public virtual object[] Arguments
        {
            get { return this.arguments; }
        }


        public virtual string Key
        {
            get { return this.key; }
        }


        public virtual string GetLocalizedMessage()
        {
            return GetLocalizedMessage(CultureInfo.CurrentUICulture);
        }


        public virtual string GetLocalizedMessage(CultureInfo locale)
        {
            return NLS.GetLocalizedMessage(Key, locale, Arguments);
        }


        public override string ToString()
        {
            object[] args = Arguments;
            StringBuilder sb = new StringBuilder(Key);
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    sb.Append(i == 0 ? " " : ", ").Append(args[i]);
                }
            }
            return sb.ToString();
        }
    }
}