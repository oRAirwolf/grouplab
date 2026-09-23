# 2026-09-23, entry 151: the Community link goes to a page, not straight out to Discord

Alan: "make it so the community link at the top of the page does not automatically redirect to the
discord server. People will not appreciate this. Make a page that has a link to the invite and let
people choose if they want to click on it."

He is right. `website/_site/discord/index.html` is a meta refresh that throws the visitor off
grouplab.org before they have read anything, and a navigation item that ejects you from the site is
the kind of thing people remember badly. Entry 148 asked for a canonical address that redirects, and
this entry supersedes that part of it. The canonical address stays. The redirect goes.

## 1. What replaces it

`https://grouplab.org/discord` becomes a real page on the site, in the site's own layout, with the
navigation, the footer and the theme. Nothing about it navigates on its own. It contains:

1. A short paragraph saying what the Discord is for: asking questions, reporting what a build did,
   showing targets, and talking to whoever is working on it.
2. The channel list in plain words, so somebody can see what they are joining before they join. Group
   them the way the server does: Information, Using GroupLab, Shooting, Development, Voice.
3. **One clearly marked button or link that opens the invite**, and that is the only thing on the page
   that leaves the site. It says where it goes, in words, before it is clicked: "Opens Discord in a new
   tab". The invite address is visible as text as well as being the link target, because some people
   want to see where a link goes before they follow it.
4. A line saying Discord is a third party service with its own account requirement and its own terms,
   and that nobody has to join it to use GroupLab or to get help. Point at `support@grouplab.org` and
   at the GitHub issues as the alternatives.
5. A short summary of the server rules, or a link to them, so the rules are readable before joining
   rather than only after.

## 2. Naming

The navigation item currently says "Community". Keep that word rather than "Discord", because the page
is the community page and Discord is one thing on it. In the footer, where the item currently says
"Discord", change it to "Community" as well so the two agree.

## 3. The single source stays

`website/links.json` keeps `discordInvite` and `discordCanonical`. Everything published still points at
the canonical address, which is now a page rather than a redirect. Nothing else on the site or in the
README carries the `discord.gg` address, with the one exception of the visible link text on this page
itself. Keep the test that enforces that, and amend it for the exception rather than deleting it.

## 4. Test

1. A test that fails if any page on the built site carries a `<meta http-equiv="refresh">`. That is the
   general form of the fault and it will catch the next one too.
2. A test that the community page contains the invite as a link and as visible text, and that it is the
   only page that does.
