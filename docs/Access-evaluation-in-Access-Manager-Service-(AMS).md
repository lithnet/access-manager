AMS uses the built-in Windows authorization system to determine if a user is authorized to access a resource.

When a user requests access to a computer, AMS will obtain an 'identification token' from the domain where the _computer_ resource is located. This identification token will contain the group membership of the user, from the perspective of the computer that is being accessed. All groups that are visible from the computer's domain will be included in the token. This is similar to a token a user would get had they logged onto that computer directly.

This means that depending on the location of the resource the user is accessing, you'll need to place your access control rules accordingly.

## Within the same domain
When the requesting user and computer are in the same domain, the user's access token will contain all the domain local, global, and universal groups memberships from that domain. No special consideration is needed in this case.

## Within the same forest
When the requesting user and computer are in different domains in the same forest, the user's access token will contain global and universal groups from the forest, as well as domain local groups from the domain where the computer is located.

Domain local groups from the user's home domain are not present. Therefore, a user must be granted access to the resource via a global or universal group from that forest

## Across forests with a two-way trust
If the user and computer are in different forests, provided there is a two-way trust in place then the user will have their group membership built in the other forest. The user will need to be a member of a domain local group in that domain in order to gain access.

## Access across a forest with a one-way trust
When a one-way forest trust is in place, access control must work a little differently. Let's call the forest containing AMS the RED, and the forest containing the computer is GREEN. 

In this scenario, the GREEN forest trusts the RED forest, but the RED forest does not trust the GREEN forest. Users from the RED forest are able to login to the GREEN forest and access objects within. Users from the GREEN forest can't access resources within the RED forest.

AMS uses Kerberos S4U2Self in order to obtain the 'identification token' mentioned earlier. Unfortunately, S4U is not supported across a one-way trust. When AMS detects a request that would cross a one-way trust, it modifies the way it performs the access checks. 

When a user from the RED forest tries to obtain access to a computer from the GREEN forest, AMS will use the user's home domain (rather than the computer's domain) to build the access token. This means that any group membership that the user may have in the GREEN forest is not seen at all when evaluating access to a resource in the GREEN forest. 

In order to support users from the RED forest accessing computers in the GREEN forest, you must ensure that a group from the RED forest is present on the ACL granting access to the computers in the GREEN forest.