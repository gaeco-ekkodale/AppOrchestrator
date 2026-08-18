# Frontend Getting Started

This guide helps you set up and run the React frontend for AppOrchestrator.

## Prerequisites

- **Node.js**: Version 18 or higher.
- **npm**: Included with Node.js.

## Installation

Navigate to the `Client` directory and install dependencies:

```bash
cd Client
npm install
```

## Running Development Server

Start the Vite development server:

```bash
npm run dev
```

The application will be available at `http://localhost:3000` (or the port shown in the terminal).

## Building for Production

To build the application for production:

```bash
npm run build
```

The output will be in the `dist` directory.

## Linting

To run the linter:

```bash
npm run lint
```

## Environment Variables

The frontend is configured via `.env.development` (local) and `.env.production` (production). Key variables:

| Variable       | Description                         | Example                 |
| :------------- | :---------------------------------- | :---------------------- |
| `VITE_API_URL` | Base URL of the AppOrchestrator API | `http://localhost:6241` |
