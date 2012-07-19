using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Editor.Model
{
    public class DoubleLinkedListNode<T>
    {
        private T value;
        private DoubleLinkedListNode<T> previous;
        private DoubleLinkedListNode<T> next;

        public DoubleLinkedListNode(T value)
        {
            this.value = value;
        }

        public DoubleLinkedListNode(T value, DoubleLinkedListNode<T> previous)
            : this(value)
        {
            this.previous = previous;
        }
        public DoubleLinkedListNode(T value, DoubleLinkedListNode<T> previous, DoubleLinkedListNode<T> next)
            : this(value, previous)
        {
            this.next = next;
        }

        public DoubleLinkedListNode<T> Previous
        {
            get { return previous; }
            set { previous = value; }
        }

        public DoubleLinkedListNode<T> Next
        {
            get { return next; }
            set { next = value; }
        }

        public T Value
        {
            get { return value; }
            set { this.value = value; }
        }
    }
}
