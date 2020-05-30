using System;
using System.Collections.Immutable;

namespace Lithnet.Laps.Web.Audit
{
    public sealed class UsersToNotify
    {
        public IImmutableSet<string> OnSuccess { get; private set; }

        public IImmutableSet<string> OnFailure { get; private set; }

        public IImmutableSet<string> All => this.OnSuccess.Union(this.OnFailure);

        public UsersToNotify()
        {
            this.OnSuccess = ImmutableHashSet<string>.Empty;
            this.OnFailure = ImmutableHashSet<string>.Empty;
        }

        public UsersToNotify(IImmutableSet<string> onSuccess, IImmutableSet<string> onFailure)
        {
            this.OnSuccess = onSuccess;
            this.OnFailure = onFailure;
        }

        public UsersToNotify NotifyOnSuccess(IImmutableSet<string> emails)
        {
            return new UsersToNotify(this.OnSuccess.Union(emails), this.OnFailure);
        }

        public UsersToNotify NotifyOnFailure(IImmutableSet<string> emails)
        {
            return new UsersToNotify(this.OnSuccess, this.OnFailure.Union(emails));
        }

        public UsersToNotify Union(UsersToNotify other)
        {
            return new UsersToNotify(this.OnSuccess.Union(other.OnSuccess), this.OnFailure.Union(other.OnFailure));
        }

        private static IImmutableSet<string> GetUsersFromString(string commaSeparatedEmails)
        {
            if (string.IsNullOrWhiteSpace(commaSeparatedEmails))
            {
                return ImmutableHashSet<string>.Empty;
            }

            string[] users = commaSeparatedEmails.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

            return ImmutableHashSet<string>.Empty.Union(users);
        }

        public UsersToNotify NotifyOnSuccess(string commaSeparatedEmails)
        {
            return this.NotifyOnSuccess(UsersToNotify.GetUsersFromString(commaSeparatedEmails));
        }

        public UsersToNotify NotifyOnFailure(string commaSeparatedEmails)
        {
            return this.NotifyOnFailure(UsersToNotify.GetUsersFromString(commaSeparatedEmails));
        }

        public UsersToNotify(string onSuccess, string onError) : this(UsersToNotify.GetUsersFromString(onSuccess),
            UsersToNotify.GetUsersFromString(onError))
        {
        }

        public UsersToNotify WithUserReplaced(string oldEmail, string newEmail)
        {
            return new UsersToNotify(this.OnSuccess.Remove(oldEmail).Add(newEmail), this.OnFailure.Remove(oldEmail).Add(newEmail)
            );
        }
    }
}