import { agendaAccountConfigResponse, agendaAccountOptionsResponse } from "../../../../lib/agenda-account-server";

export async function GET() {
  return agendaAccountConfigResponse();
}

export async function OPTIONS() {
  return agendaAccountOptionsResponse("GET, OPTIONS");
}
