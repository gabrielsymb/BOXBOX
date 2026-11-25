using System;

namespace BoxBox.Domain
{
    public readonly struct Money : IEquatable<Money>, IComparable<Money>
    {
        public long Cents { get; }

        public Money(long cents)
        {
            Cents = cents;
        }

        public static Money Zero => new Money(0);

        public static Money operator +(Money left, Money right)
        {
            return new Money(checked(left.Cents + right.Cents));
        }

        public static Money operator -(Money left, Money right)
        {
            return new Money(checked(left.Cents - right.Cents));
        }

        public int CompareTo(Money other)
        {
            return Cents.CompareTo(other.Cents);
        }

        public bool Equals(Money other)
        {
            return Cents == other.Cents;
        }

        public override bool Equals(object obj)
        {
            return obj is Money other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Cents.GetHashCode();
        }
    }
}
