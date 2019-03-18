using System.Collections.Immutable;

namespace Lithnet.Laps.Web.Audit
{
    public class UsersToBeNotified
    {
        public IImmutableSet<string> OnSuccess { get; private set; }
        public IImmutableSet<string> OnFailure { get; private set; }

        public UsersToBeNotified(IImmutableSet<string> onSuccess, IImmutableSet<string> onFailure)
        {
            OnSuccess = onSuccess;
            OnFailure = onFailure;
        }

        public UsersToBeNotified NotifyOnSuccess(IImmutableSet<string> emails)
        {
            return new UsersToBeNotified(OnSuccess.Union(emails), OnFailure);
        }

        public UsersToBeNotified NotifyOnFailure(IImmutableSet<string> emails)
        {
            return new UsersToBeNotified(OnSuccess, OnFailure.Union(emails));
        }
    }
}