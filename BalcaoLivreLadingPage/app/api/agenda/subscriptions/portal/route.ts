import {
  agendaSubscriptionErrorResponse,
  agendaSubscriptionOptionsResponse,
  createAgendaSubscriptionPortal,
} from "../../../../lib/agenda-subscription-server";

export async function POST(request: Request) {
  try {
    return await createAgendaSubscriptionPortal(request);
  } catch (error) {
    return agendaSubscriptionErrorResponse(error);
  }
}

export async function OPTIONS() {
  return agendaSubscriptionOptionsResponse("POST, OPTIONS");
}
