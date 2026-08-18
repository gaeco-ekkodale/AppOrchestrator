// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {TourPanel} from "./Tour";

export const TOUR_KEY = "apporchestrator";
export const TOUR_MODULE_NAME = "App Orchestrator";

/**
 * Deliberately short. Setting up gaeco does not involve this module, and it is not where a
 * new user should spend time - the panels only explain what it is for.
 */
export const TOUR_PANELS: TourPanel[] = [
  {
    title: "Where gaeco grows",
    body: "Setting up gaeco does not involve this module. You only need it when the platform should do something it cannot do yet.",
  },
  {
    title: "Adding a module",
    body: "Deploy one from the app store of a connected registry; it then appears in the app menu like the built-in modules. Everything deployed is managed under Stacks.",
  },
];
