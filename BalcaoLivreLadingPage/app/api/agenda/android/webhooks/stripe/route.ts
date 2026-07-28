import {
  agendaAndroidErrorResponse,
  handleAgendaAndroidStripeWebhook,
} from "../../../../../lib/agenda-android-server";

export async function POST(request: Request) {
  try {
    return await handleAgendaAndroidStripeWebhook(request);
  } catch (error) {
    return agendaAndroidErrorResponse(error);
  }
}
