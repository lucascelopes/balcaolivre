import {
  agendaSubscriptionErrorResponse,
  agendaSubscriptionOptionsResponse,
  getAgendaSubscriptionCheckoutStatus,
} from "../../../../lib/agenda-subscription-server";

export async function GET(request: Request) {
  try {
    return await getAgendaSubscriptionCheckoutStatus(request);
  } catch (error) {
    return agendaSubscriptionErrorResponse(error);
  }
}

export async function OPTIONS() {
  return agendaSubscriptionOptionsResponse("GET, OPTIONS");
}
