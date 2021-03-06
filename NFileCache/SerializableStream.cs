﻿/*
Copyright 2015 mabakay, based on FileCache (http://fc.codeplex.com)

This file is part of nFileCache (https://github.com/mabakay/nFileCache).

nFileCache is distributed under the Microsoft Public License (Ms-PL).
Consult "LICENSE.txt" included in this package for the complete Ms-PL license.
*/

using System.IO;

namespace System.Runtime.Caching
{
    [Serializable]
    internal sealed class SerializableStream
    {
        #region Properties

        public byte[] Data { get; private set; }

        #endregion

        #region Constructors

        public SerializableStream(Stream stream)
        {
            using (var ms = new MemoryStream((int)stream.Length))
            {
                stream.CopyTo(ms);

                Data = ms.GetBuffer();
            }
        } 

        #endregion
    }
}