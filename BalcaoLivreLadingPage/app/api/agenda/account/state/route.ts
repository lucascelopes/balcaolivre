import {
  agendaAccountErrorResponse,
  agendaAccountOptionsResponse,
  getAgendaAccountState,
  putAgendaAccountState,
} from "../../../../lib/agenda-account-server";

export async function GET(request: Request) {
  try {
    return await getAgendaAccountState(request);
  } catch (error) {
    return agendaAccountErrorResponse(error);
  }
}

export async function PUT(request: Request) {
  try {
    return await putAgendaAccountState(request);
  } catch (error) {
    return agendaAccountErrorResponse(error);
  }
}

export async function OPTIONS() {
  return agendaAccountOptionsResponse();
}
