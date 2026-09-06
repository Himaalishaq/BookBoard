# 📖 BookBoard

**BookBoard** is a Pinterest-inspired full-stack web app that reimagines book discovery beyond traditional genre-based searching. Instead of only browsing by categories like fantasy, romance, or mystery, users can create aesthetic mood-based reading boards and discover books based on their current **mood**, **vibe**, and **emotional reading preference**.

**Live Demo:** https://bookboard.onrender.com  
> Note: Hosted on Render’s free tier, so the app may take a few seconds to wake up after inactivity.

---

## Project Overview

Most book discovery platforms organize recommendations around broad categories like genre, author, bestseller lists, or popularity. While useful, those filters do not always reflect how readers actually choose books.

BookBoard takes a more personal and visual approach.

Users can create reading boards around moods such as **cozy winter**, **dark academia**, **emotional healing**, **spiritual reflection**, or **summer reads**. Each board can include books, mood tags, reflections, and visual collage-style elements, helping users organize and discover books based on how they want a reading experience to feel.

The goal of this project was to build and deploy a full-stack web application with authentication, database-backed user content, recommendation-style discovery, and a polished user interface.

---

## Key Features

- **Mood-Based Reading Boards**  
  Users can create public or private boards organized by mood, aesthetic, emotion, or reading vibe.

- **Pinterest-Inspired Visual Collages**  
  Boards use visual collage-style previews with book covers, quotes, colors, symbols, and aesthetic tiles.

- **Vibe-Based Book Discovery**  
  Users can search for boards and books using moods such as `cozy`, `gloomy`, `healing`, `reflective`, or `dark academia`.

- **Recommendation Logic**  
  BookBoard recommends books and boards based on overlapping mood tags, board themes, and shared reading vibes.

- **User Authentication**  
  Users can register, log in, create boards, save boards, and manage their personal reading profile.

- **Reading Profile**  
  Each user has a profile showing saved books, created boards, favorite moods, recent activity, and a generated taste profile.

- **Deployed Web App**  
  The project is deployed publicly using Docker and Render, making it accessible outside of a local development environment.

---

## Tech Stack

**Frontend:** Razor Pages, HTML, CSS  
**Backend:** ASP.NET Core, C#  
**Database:** SQLite with Entity Framework Core  
**Authentication:** ASP.NET Core Identity  
**Deployment:** Docker, Render  
**Tools:** Git, GitHub, VS Code, .NET CLI

---

## What Makes BookBoard Different

Traditional book apps often focus on genre-based search. BookBoard focuses on **mood-first discovery**.

Instead of asking:

> “What genre do you want to read?”

BookBoard asks:

> “What kind of reading experience are you in the mood for?”

This allows users to discover books through emotional tone, aesthetic style, and personal reading context, making the experience more visual, expressive, and personalized.

---


## Core Data Models

BookBoard uses several connected models to support authentication, boards, books, saved content, visual customization, and recommendations:

- `ApplicationUser`
- `Board`
- `BoardBook`
- `SavedBoard`
- `BoardVisualItem`
- `Tag`
- `BoardTag`
- `BookTag`

These models allow users to create boards, attach books to boards, save other users’ boards, customize board visuals, and generate recommendations using mood-based tags.



## Running Locally

Clone the repository:

```bash
git clone https://github.com/Himaalishaq/BookBoard.git
cd BookBoard
```

Restore dependencies:

```bash
dotnet restore
```

Apply database migrations:

```bash
dotnet ef database update
```

Run the application:

```bash
dotnet run
```

Open the app in your browser:

```text
http://localhost:5166
```

---

## Deployment

BookBoard is deployed using **Docker** and **Render**.

The deployment process included:

- Creating a Dockerfile for the ASP.NET Core application.
- Publishing the app in Release mode.
- Configuring the app to run on Render’s hosted environment.
- Debugging deployment-specific issues related to environment variables, static file handling, and database migrations.
- Making the application publicly accessible through a Render URL.

---

## Future Improvements

- Improve recommendation logic with stronger tag weighting.
- Add image upload support instead of only image URLs.
- Add user following and public profile pages.
- Add comments or reactions on public boards.
- Upgrade from SQLite to a persistent cloud database for production use.
- Improve mobile responsiveness and accessibility.




## Project Status

BookBoard is a deployed portfolio project and active work-in-progress. Core features including authentication, board creation, visual collage previews, mood-based discovery, saved boards, recommendations, and reading profiles are functional.

The project was built to demonstrate full-stack development, product thinking, database design, authentication, deployment, and user-centered feature development.
