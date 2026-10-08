/**
 * Production build (ng build / Render static site).
 * apiUrl = the Render web service URL of the API. If Render gives your API a different
 * name than "onemoi-api", change it here and push.
 */
export const environment = {
  apiUrl: 'https://onemoi-api.onrender.com',
  /** Never list demo passwords on the public website */
  demoLogins: [] as { who: string; mode: string; login: string; secret: string; tenant?: string }[]
};