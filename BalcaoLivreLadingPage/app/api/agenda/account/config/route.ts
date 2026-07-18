import {
  agendaAccountConfigResponse,
  agendaAccountErrorResponse,
  agendaAccountOptionsResponse,
} from "../../../../lib/agenda-account-server";

export async function GET() {
  try {
    return agendaAccountConfigResponse();
  } catch (error) {
    return agendaAccountErrorResponse(error);
  }
}

export async function OPTIONS() {
  return agendaAccountOptionsResponse("GET, OPTIONS");
}
