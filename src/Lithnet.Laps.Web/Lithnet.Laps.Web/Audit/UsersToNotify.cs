using System;
using System.Collections.Immutable;

namespace Lithnet.Laps.Web.Audit
{
    public sealed class UsersToNotify
    {
        public IImmutableSet<string> OnSuccess { get; private set; }
        public IImmutableSet<string> OnFailure { get; private set; }

        public UsersToNotify()
        {
            OnSuccess = ImmutableHashSet<string>.Empty;
            OnFailure = ImmutableHashSet<string>.Empty;
        }

        public UsersToNotify(IImmutableSet<string> onSuccess, IImmutableSet<string> onFailure)
        {
            OnSuccess = onSuccess;
            OnFailure = onFailure;
        }

        public UsersToNotify NotifyOnSuccess(IImmutableSet<string> emails)
        {
            return new UsersToNotify(OnSuccess.Union(emails), OnFailure);
        }

        public UsersToNotify NotifyOnFailure(IImmutableSet<string> emails)
        {
            return new UsersToNotify(OnSuccess, OnFailure.Union(emails));
        }

        public UsersToNotify Union(UsersToNotify other)
        {
            return new UsersToNotify(OnSuccess.Union(other.OnSuccess), OnFailure.Union(other.OnFailure));
        }

        private static IImmutableSet<string> GetUsersFromString(string commaSeparatedEmails)
        {
            if (string.IsNullOrWhiteSpace(commaSeparatedEmails))
            {
                return ImmutableHashSet<string>.Empty;
            }

            var users = commaSeparatedEmails.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

            return ImmutableHashSet<string>.Empty.Union(users);
        }

        public UsersToNotify NotifyOnSuccess(string commaSeparatedEmails)
        {
            return NotifyOnSuccess(GetUsersFromString(commaSeparatedEmails));
        }

        public UsersToNotify NotifyOnFailure(string commaSeparatedEmails)
        {
            return NotifyOnFailure(GetUsersFromString(commaSeparatedEmails));
        }

        public UsersToNotify(string onSuccess, string onError) : this(GetUsersFromString(onSuccess),
            GetUsersFromString(onError))
        {
        }

        public IImmutableSet<string> All => OnSuccess.Union(OnFailure);

        public UsersToNotify WithUserReplaced(string oldEmail, string newEmail)
        {
            return new UsersToNotify(
                OnSuccess.Remove(oldEmail).Add(newEmail),
                OnFailure.Remove(oldEmail).Add(newEmail)
            );
        }
    }
}