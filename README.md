# Whack-A-Mole Users Service

This is the users service of the Whack-A-Mole Microservices game. This Users service us responsible for user registration. The game does not work with an identity provider or other trustworthy way of maintaining users, mainly because there is no need to do so. When a user enters the game, the game front-end will make a backend request to either verify an earlier created user ID, or receive a newly created user ID. This user ID is then stored locally and used from there one.

> Note that this way of working with users is not secure and can be easily tampered with. For the sake of this project it is not required to have a safe and secure way of identifying individuals. If you do want that, I advise to integrate with an identity provider.

## User registration

When a user ID was newly created, the user is asked to enter a name and email address. This is passed to the server and the server takes this information to be able to show a leader board.

## Banning and / or locking out users

This game is often demonstrated as a demo at conferences and meetups. It is important to understand that user names must comply with the Code of Conduct for that conference. If users come with a name that is not compliant with the conference Code of Conduct, a secured backend request can lock-out users and forbid them from joining the game.

## Storage

The users service takes advantags of Azure Table Storage to store user information. A redis cache cluster (distributed cache) allows for storing user information in the cache. Implementing the cache-aside pattern allows to first return data from cache, before consulting the underlying data store.
