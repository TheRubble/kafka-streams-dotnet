﻿using Streamiz.Kafka.Net.Crosscutting;
using Streamiz.Kafka.Net.State.RocksDb.Internal;
using System.Collections;
using System.Collections.Generic;

namespace Streamiz.Kafka.Net.State.Enumerator
{
    internal class WindowStoreEnumeratorWrapper
    {
        internal class WrappedWindowStoreEnumerator : IWindowStoreEnumerator<byte[]>
        {
            private readonly IKeyValueEnumerator<Bytes, byte[]> bytesEnumerator;

            public WrappedWindowStoreEnumerator(IKeyValueEnumerator<Bytes, byte[]> bytesEnumerator)
            {
                this.bytesEnumerator = bytesEnumerator;
            }

            public KeyValuePair<long, byte[]>? Current
            {
                get
                {
                    var current = bytesEnumerator.Current;
                    if (current.HasValue)
                    {
                        long ts = RocksDbWindowKeySchema.ExtractStoreTimestamp(current.Value.Key.Get);
                        return KeyValuePair.Create(ts, current.Value.Value);
                    }
                    else
                        return null;
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
                => bytesEnumerator.Dispose();

            public bool MoveNext()
                => bytesEnumerator.MoveNext();

            public long PeekNextKey()
                => RocksDbWindowKeySchema.ExtractStoreTimestamp(bytesEnumerator.PeekNextKey().Get);

            public void Reset()
                => bytesEnumerator.Reset();
        }

        internal class WrappedKeyValueEnumerator : IKeyValueEnumerator<Windowed<Bytes>, byte[]>
        {
            private readonly IKeyValueEnumerator<Bytes, byte[]> bytesEnumerator;
            private readonly long windowSize;

            public WrappedKeyValueEnumerator(IKeyValueEnumerator<Bytes, byte[]> bytesEnumerator,
                long windowSize)
            {
                this.bytesEnumerator = bytesEnumerator;
                this.windowSize = windowSize;
            }

            public KeyValuePair<Windowed<Bytes>, byte[]>? Current
            {
                get
                {
                    if (bytesEnumerator.Current.HasValue)
                    {
                        var key = bytesEnumerator.Current?.Key.Get;
                        var k = RocksDbWindowKeySchema.FromStoreBytesKey(key, windowSize);
                        return KeyValuePair.Create(k, bytesEnumerator.Current.Value.Value);
                    }
                    else
                        return null;
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
                  => bytesEnumerator.Dispose();

            public bool MoveNext()
                => bytesEnumerator.MoveNext();

            public Windowed<Bytes> PeekNextKey()
            {
                if (bytesEnumerator.Current.HasValue)
                {
                    var key = bytesEnumerator.Current?.Key.Get;
                    return RocksDbWindowKeySchema.FromStoreBytesKey(key, windowSize);
                }
                else
                    return null;
            }

            public void Reset()
                => bytesEnumerator.Reset();
        }

        private readonly IKeyValueEnumerator<Bytes, byte[]> bytesEnumerator;
        private readonly long windowSize;

        internal WindowStoreEnumeratorWrapper(IKeyValueEnumerator<Bytes, byte[]> bytesEnumerator,
                                    long windowSize)
        {
            this.bytesEnumerator = bytesEnumerator;
            this.windowSize = windowSize;
        }

        public IWindowStoreEnumerator<byte[]> ToWindowStoreEnumerator()
            => new WrappedWindowStoreEnumerator(bytesEnumerator);

        public IKeyValueEnumerator<Windowed<Bytes>, byte[]> ToKeyValueEnumerator()
            => new WrappedKeyValueEnumerator(bytesEnumerator, windowSize);
    }
}