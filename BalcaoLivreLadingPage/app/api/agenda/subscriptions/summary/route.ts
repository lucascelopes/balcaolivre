import {
  agendaSubscriptionErrorResponse,
  agendaSubscriptionOptionsResponse,
  getAgendaSubscriptionSummary,
} from "../../../../lib/agenda-subscription-server";

export async function GET(request: Request) {
  try {
    return await getAgendaSubscriptionSummary(request);
  } catch (error) {
    return agendaSubscriptionErrorResponse(error);
  }
}

export async function OPTIONS() {
  return agendaSubscriptionOptionsResponse("GET, OPTIONS");
}
