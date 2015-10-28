# nFileCache
NFileCache is a implementation of the .NET System.Runtime.Caching.ObjectCache that uses the filesystem as the target location.

Welcome to the NFileCache! It is a fork of adamcarter's [.NET File Cache](http://www.google.pl) with a handful of extra features.

##Getting Started: Basic usage

```c#
var cache = new NFileCache();
cache["foo"] = "bar";

Console.WriteLine("Reading foo from cache: {0}", cache["foo"]);
```

By default NFileCache internally uses .NET binary serializer and supports all value types, streams and classes marked with Serializable attribute. Optionally you can extend or change this behavior by providing own implementation of serializer class.

Checkout the [wiki](https://github.com/mabakay/nFileCache/wiki) for more documentation.
